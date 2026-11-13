using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using MySql.Data.MySqlClient;

namespace BodeGo.API.Shared.Infrastructure.Hosting;

public static class BodeGoHosting
{
    public const string FrontendPolicy = "BodeGoFrontend";

    public static IServiceCollection AddBodeGoHosting(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0 && environment.IsDevelopment())
            origins = ["http://localhost:5174"];
        if (origins.Length == 0)
            throw new InvalidOperationException("Configure Cors:AllowedOrigins para el frontend publicado.");
        foreach (var origin in origins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || uri.Scheme is not ("http" or "https") || uri.AbsolutePath != "/"
                || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
                || !string.IsNullOrEmpty(uri.UserInfo) || uri.Host.Contains('*')
                || origin != uri.GetLeftPart(UriPartial.Authority)
                || (!environment.IsDevelopment() && uri.Scheme != "https"))
                throw new InvalidOperationException("Cada origen CORS debe ser una URL exacta sin ruta ni barra final; use HTTPS en producción.");
        }
        services.AddCors(options => options.AddPolicy(FrontendPolicy,
            policy => policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader()));

        var proxies = configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
        var networks = configuration.GetSection("ReverseProxy:KnownIPNetworks").Get<string[]>() ?? [];
        var limit = configuration.GetValue("ReverseProxy:ForwardLimit", 1);
        if (limit is < 1 or > 5)
            throw new InvalidOperationException("ReverseProxy:ForwardLimit debe estar entre 1 y 5.");
        var addresses = proxies.Select(value => IPAddress.TryParse(value, out var ip) ? ip
            : throw new InvalidOperationException("ReverseProxy:KnownProxies contiene una IP no válida.")).ToArray();
        var ranges = networks.Select(value => System.Net.IPNetwork.TryParse(value, out var network) && network.PrefixLength > 0 ? network
            : throw new InvalidOperationException("ReverseProxy:KnownIPNetworks requiere redes CIDR específicas.")).ToArray();
        foreach (var key in new[] { "ASPNETCORE_FORWARDEDHEADERS_ENABLED", "DOTNET_FORWARDEDHEADERS_ENABLED", "ForwardedHeaders_Enabled" })
            if (configuration.GetValue(key, false))
                throw new InvalidOperationException("No habilite forwarded headers sin restricciones; configure ReverseProxy.");
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = limit;
            // Se conservan los valores loopback seguros del framework.
            foreach (var address in addresses)
            {
                options.KnownProxies.Add(address);
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    options.KnownProxies.Add(address.MapToIPv6());
            }
            foreach (var network in ranges)
            {
                options.KnownIPNetworks.Add(network);
                if (network.BaseAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    options.KnownIPNetworks.Add(new System.Net.IPNetwork(network.BaseAddress.MapToIPv6(), 96 + network.PrefixLength));
            }
        });
        if (!environment.IsDevelopment()) _ = DatabaseConnection(configuration, isDevelopment: false);
        return services;
    }

    public static string DatabaseConnection(IConfiguration configuration, bool isDevelopment)
    {
        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection mediante configuración segura.");
        if (!isDevelopment)
        {
            MySqlConnectionStringBuilder settings;
            try { settings = new MySqlConnectionStringBuilder(connection); }
            catch (Exception e) when (e is ArgumentException or FormatException or OverflowException)
            { throw new InvalidOperationException("ConnectionStrings:DefaultConnection no tiene un formato MySQL válido."); }
            if (string.IsNullOrWhiteSpace(settings.Server) || string.IsNullOrWhiteSpace(settings.Database)
                || settings.ConnectionProtocol != MySqlConnectionProtocol.Sockets
                || settings.Server.Split(',').Any(IsLocalHost))
                throw new InvalidOperationException("Producción requiere un host MySQL remoto explícito y una base configurada.");
        }
        return connection;
    }

    private static bool IsLocalHost(string value)
    {
        var host = value.Trim().TrimEnd('.');
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.Equals("localhost.localdomain", StringComparison.OrdinalIgnoreCase)
            || host is "." or "" or "(local)" || host.StartsWith('/')) return true;
        if (!IPAddress.TryParse(host, out var ip)) return false;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        return IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any);
    }

    public static void UseBodeGoTransport(this WebApplication app)
    {
        app.UseForwardedHeaders();
        if (!app.Environment.IsDevelopment()) app.UseHsts();
        // Render ya redirige HTTP a HTTPS en su borde y entrega HTTP al contenedor.
        // No redirigir de nuevo evita bucles cuando aún falta configurar el proxy.
        if (!app.Environment.IsProduction() || !app.Configuration.GetValue("RENDER", false))
            app.UseHttpsRedirection();
        else if (!(app.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? []).Any()
            && !(app.Configuration.GetSection("ReverseProxy:KnownIPNetworks").Get<string[]>() ?? []).Any())
            app.Logger.LogWarning("Configure ReverseProxy con los proxies verificados de Render para reconocer esquema HTTPS e IP del cliente. El límite de login puede compartirse entre clientes mientras tanto.");
    }
}
