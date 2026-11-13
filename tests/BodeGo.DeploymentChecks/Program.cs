using System.Net;
using System.Net.Http.Json;
using System.Text;
using BodeGo.API.Shared.Infrastructure.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var checks = 0;
void Check(bool valid, string message)
{
    if (!valid) throw new InvalidOperationException(message);
    checks++;
}
void Reject(Action action, string message)
{
    try { action(); }
    catch (InvalidOperationException) { checks++; return; }
    throw new InvalidOperationException(message);
}
IConfiguration Configuration(Dictionary<string, string?>? overrides = null)
{
    var values = new Dictionary<string, string?>
    {
        ["Cors:AllowedOrigins:0"] = "https://frontend.example",
        ["ConnectionStrings:DefaultConnection"] = "Server=mysql.example.invalid;Database=bodego_demo;User=unused;SslMode=VerifyFull;",
        ["ReverseProxy:KnownProxies:0"] = "10.20.30.40"
    };
    if (overrides is not null) foreach (var pair in overrides) values[pair.Key] = pair.Value;
    return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
ServiceProvider Services(IConfiguration config, string environment = "Production")
{
    var services = new ServiceCollection().AddLogging();
    services.AddBodeGoHosting(config, new CheckEnvironment(environment));
    return services.BuildServiceProvider();
}
using var provider = Services(Configuration(new() { ["ReverseProxy:KnownIPNetworks:0"] = "10.90.0.0/24" }));
var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
Check(options.ForwardLimit == 1, "Se debe limitar la cadena de proxies.");
Check(options.KnownProxies.Contains(IPAddress.Parse("10.20.30.40")), "No se configuró el proxy exacto.");
Check(options.KnownIPNetworks.Any(n => n.Contains(IPAddress.Parse("10.90.0.7"))), "No se configuró la red verificada.");
Check(options.KnownIPNetworks.Any(n => n.Contains(IPAddress.Parse("10.90.0.7").MapToIPv6())), "La red no admite sockets IPv4 mapeados.");
Check(!options.KnownIPNetworks.Any(n => n.Contains(IPAddress.Parse("203.0.113.99"))), "Se confía en una red ajena.");

foreach (var origin in new[] { "*", "http://frontend.example", "https://frontend.example/", "https://frontend.example/path", "https://frontend.example?x=1" })
    Reject(() => { using var p = Services(Configuration(new() { ["Cors:AllowedOrigins:0"] = origin })); }, "Se aceptó un origen inválido.");
Reject(() => { using var p = Services(Configuration(new() { ["Cors:AllowedOrigins:0"] = null })); }, "Se aceptó CORS vacío en Production.");
foreach (var host in new[] { "localhost", "127.0.0.1", "::1", "::ffff:127.0.0.1", "0.0.0.0", "localhost." })
    Reject(() => BodeGoHosting.DatabaseConnection(Configuration(new() { ["ConnectionStrings:DefaultConnection"] = "Server=" + host + ";Database=bodego_demo;User=unused;" }), false),
        "Se aceptó un host local en Production.");
Reject(() => BodeGoHosting.DatabaseConnection(Configuration(new() { ["ConnectionStrings:DefaultConnection"] = "" }), false), "Se aceptó conexión vacía.");
Check(BodeGoHosting.DatabaseConnection(Configuration(), false).Contains("mysql.example.invalid"), "Se cambió la conexión remota.");
Check(BodeGoHosting.DatabaseConnection(Configuration(new() { ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=bodego_db;" }), true).Contains("localhost"),
    "Se rompió el desarrollo local sin abrir MySQL.");
Reject(() => { using var p = Services(Configuration(new() { ["ReverseProxy:KnownProxies:0"] = "no-es-ip" })); }, "Se aceptó una IP inválida.");
Reject(() => { using var p = Services(Configuration(new() { ["ReverseProxy:KnownIPNetworks:0"] = "0.0.0.0/0" })); }, "Se aceptó confianza universal.");
Reject(() => { using var p = Services(Configuration(new() { ["ReverseProxy:ForwardLimit"] = "0" })); }, "Se aceptó un límite inválido.");
Reject(() => { using var p = Services(Configuration(new() { ["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true" })); }, "Se aceptó confianza automática sin restricciones.");

using (var dev = Services(new ConfigurationBuilder().Build(), "Development"))
{
    var cors = dev.GetRequiredService<IOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>>().Value.GetPolicy(BodeGoHosting.FrontendPolicy)!;
    Check(cors.Origins.SequenceEqual(["http://localhost:5174"]) && !cors.SupportsCredentials && !cors.AllowAnyOrigin, "CORS local cambió.");
}

async Task<DefaultHttpContext> Forward(string peer)
{
    var context = new DefaultHttpContext();
    context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("api.example");
    context.Request.Headers["X-Forwarded-For"] = "203.0.113.10";
    context.Request.Headers["X-Forwarded-Proto"] = "https";
    context.Request.Headers["X-Forwarded-Host"] = "untrusted.example";
    var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask,
        provider.GetRequiredService<ILoggerFactory>(), Options.Create(options));
    await middleware.Invoke(context);
    return context;
}
var trusted = await Forward("10.20.30.40");
Check(trusted.Request.Scheme == "https" && trusted.Connection.RemoteIpAddress!.Equals(IPAddress.Parse("203.0.113.10")), "El proxy conocido no restauró HTTPS/IP.");
Check(trusted.Request.Host.Host == "api.example", "Se aceptó X-Forwarded-Host arbitrario.");
var unknown = await Forward("203.0.113.88");
Check(unknown.Request.Scheme == "http" && unknown.Connection.RemoteIpAddress!.Equals(IPAddress.Parse("203.0.113.88")), "Se confió en encabezados de un origen desconocido.");

var docker = File.ReadAllText("Dockerfile");
var entry = File.ReadAllText("docker-entrypoint.sh");
var ignore = File.ReadAllText(".dockerignore");
Check(docker.Contains("FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build") && docker.Contains("FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime"),
    "Las imágenes no corresponden a .NET 10.");
Check(docker.Contains("--configuration Release --no-restore") && docker.Contains("COPY --from=build /out/ ./") && docker.Contains("USER $APP_UID"),
    "Falta publicación multi-stage o usuario sin privilegios.");
Check(docker.Contains("ASPNETCORE_ENVIRONMENT=Production") && docker.Contains("DOTNET_ENVIRONMENT=Production"), "Falta entorno Production.");
Check(entry.Contains("${PORT:-10000}") && entry.Contains("http://0.0.0.0:$port") && entry.Contains("exec dotnet"), "Entrada sin puerto dinámico o manejo de señales.");
Check(!entry.Contains('\r'), "El script Linux contiene CRLF.");
foreach (var item in new[] { "**/[Bb]in", "**/[Oo]bj", ".git", "**/.env", "**/.env.*", "**/secrets.json", "**/launchSettings.json", "**/*.key", "**/*.pem" })
    Check(ignore.Contains(item), "Falta exclusión Docker: " + item);

if (args is ["--api", var apiPath])
    checks += await ProductionSmoke.Run(apiPath);
else if (args.Length != 0) throw new ArgumentException("Uso: --api <BodeGo.API.dll>");

Console.WriteLine($"OK: {checks} comprobaciones de despliegue; sin consultas ni escrituras MySQL. Docker validado estáticamente, no se afirma construcción de imagen.");
sealed class CheckEnvironment(string name) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = name;
    public string ApplicationName { get; set; } = "DeploymentChecks";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
