using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Domain.Model.Aggregates;
using BodeGo.API.IAM.Domain.Model.ValueObjects;
using BodeGo.API.IAM.Domain.Repositories;
using BodeGo.API.IAM.Infrastructure;
using BodeGo.API.IAM.Infrastructure.Authentication;
using BodeGo.API.IAM.Infrastructure.Hashing;
using BodeGo.API.IAM.Interfaces.REST;
using BodeGo.API.Commercial.Application.Internal.CommandServices;
using BodeGo.API.Commercial.Application.Services;
using BodeGo.API.Commercial.Domain.Exceptions;
using BodeGo.API.Commercial.Domain.Repositories;
using BodeGo.API.Commercial.Infrastructure;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

if (args is ["--inspect-mysql"])
{
    await MySqlAuthenticationChecks.InspectExisting();
    return;
}

var checks = 0;
void Check(bool ok, string message)
{
    if (!ok) throw new InvalidOperationException(message);
    checks++;
}
var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
var hasher = new PasswordHashService();
var store = new TestRepositories();
User CreateUser(string email, int role = 2)
{
    var user = new User(role, "Prueba autenticación", new EmailAddress(email), hasher.Hash(password));
    store.Users.Add(user);
    return user;
}
var admin = CreateUser("admin@example.test", 1);
var employee = CreateUser("employee@example.test");
var other = CreateUser("other@example.test");
var inactive = CreateUser("inactive@example.test"); inactive.Deactivate();
var product = new Product(Guid.NewGuid(), new ProductName("Producto JWT"), new Money(3), new StockLevel(0));
store.Products.Add(product);
var settings = new JwtSettings { SigningKeyBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)) };
Check(settings.IsValid() && !new JwtSettings().IsValid(), "La configuración JWT debe rechazar claves vacías.");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Jwt:SigningKeyBase64"] = settings.SigningKeyBase64,
    ["Jwt:Issuer"] = settings.Issuer, ["Jwt:Audience"] = settings.Audience
});
builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
builder.Services.AddOpenApi(IamOpenApi.Configure);
builder.Services.AddIam();
builder.Services.AddIamAuthentication(builder.Configuration);
builder.Services.AddCommercial();
foreach (var type in new[] { typeof(IUserRepository), typeof(IRoleRepository), typeof(IPurchaseRepository),
    typeof(ISaleRepository), typeof(ICommercialConfirmationSession), typeof(PurchaseCommandService),
    typeof(IPurchaseCommandService), typeof(ISaleCommandService) })
    builder.Services.RemoveAll(type);
builder.Services.AddSingleton<IUserRepository>(store);
builder.Services.AddSingleton<IRoleRepository>(store);
builder.Services.AddSingleton<IPurchaseRepository>(store);
builder.Services.AddSingleton<ISaleRepository>(store);
builder.Services.AddSingleton<ICommercialConfirmationSession>(store);
builder.Services.AddSingleton(store);
builder.Services.AddScoped<IPurchaseCommandService, TestCommercialCommands>();
builder.Services.AddScoped<ISaleCommandService, TestCommercialCommands>();
builder.Services.AddCors(options => options.AddPolicy("Frontend", p => p.WithOrigins("http://localhost:5174").AllowAnyHeader().AllowAnyMethod()));
await using var app = builder.Build();
app.UseRouting();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapOpenApi();
app.MapScalarApiReference(o => o.WithTitle("BodeGo API"));
await app.StartAsync();
try
{
    using var scope = app.Services.CreateScope();
    var authentication = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
    Check(await authentication.LoginAsync(new(employee.Email.Value, "incorrecta")) is null, "Contraseña incorrecta aceptada.");
    Check(await authentication.LoginAsync(new("missing@example.test", password)) is null, "Correo inexistente aceptado.");
    Check(await authentication.LoginAsync(new(inactive.Email.Value, password)) is null, "Usuario inactivo aceptado.");
    Check(await authentication.LoginAsync(new("invalid", password)) is null, "Correo inválido aceptado.");
    Check(!hasher.Verify("hash-no-valido", password), "Hash inválido aceptado.");
    var normalized = await authentication.LoginAsync(new(" EMPLOYEE@EXAMPLE.TEST ", password));
    Check(normalized?.User.Id == employee.Id, "Login no normaliza correo.");
    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    using var http = new HttpClient { BaseAddress = new Uri(address) };
    async Task<JsonElement> Request(HttpMethod method, string path, int expected, object? body = null, string? token = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request);
        Check((int)response.StatusCode == expected, path + ": código HTTP inesperado " + (int)response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Check(!text.Contains(password) && !text.Contains("passwordHash", StringComparison.OrdinalIgnoreCase)
            && !text.Contains("password_hash", StringComparison.OrdinalIgnoreCase), "Se expusieron credenciales.");
        return JsonDocument.Parse(text).RootElement.Clone();
    }
    var login = await Request(HttpMethod.Post, "/api/v1/auth/login", 200, new { email = employee.Email.Value, password });
    var employeeToken = login.GetProperty("accessToken").GetString()!;
    Check(login.GetProperty("user").GetProperty("id").GetGuid() == employee.Id, "Identidad de login incorrecta.");
    var adminLogin = await Request(HttpMethod.Post, "/api/v1/auth/login", 200, new { email = admin.Email.Value, password });
    var adminToken = adminLogin.GetProperty("accessToken").GetString()!;
    await Request(HttpMethod.Post, "/api/v1/auth/login", 401, new { email = employee.Email.Value, password = "incorrecta" });
    await Request(HttpMethod.Post, "/api/v1/auth/login", 401, new { email = "missing@example.test", password });
    await Request(HttpMethod.Post, "/api/v1/auth/login", 401, new { email = inactive.Email.Value, password });
    await Request(HttpMethod.Post, "/api/v1/auth/login", 429, new { email = employee.Email.Value, password });
    await Request(HttpMethod.Get, "/api/v1/auth/me", 401);
    var me = await Request(HttpMethod.Get, "/api/v1/auth/me", 200, token: employeeToken);
    Check(me.GetProperty("role").GetString() == "Empleado", "Rol vigente incorrecto.");

    string Token(string? issuer = null, string? audience = null, bool expired = false, string? role = null)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim> { new("sub", employee.Id.ToString()) };
        if (role is not null) claims.Add(new("role", role));
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer ?? settings.Issuer, audience ?? settings.Audience, claims,
            expired ? now.AddHours(-2) : now, expired ? now.AddHours(-1) : now.AddMinutes(5),
            new SigningCredentials(settings.Key(), SecurityAlgorithms.HmacSha256)));
    }
    foreach (var token in new[] { Token(issuer: "otro"), Token(audience: "otra"), Token(expired: true) })
        await Request(HttpMethod.Get, "/api/v1/auth/me", 401, token: token);
    var parts = employeeToken.Split('.');
    parts[2] = (parts[2][0] == 'a' ? "b" : "a") + parts[2][1..];
    await Request(HttpMethod.Get, "/api/v1/auth/me", 401, token: string.Join('.', parts));
    var userBody = new { name = "Usuario creado por administrador", email = "created@example.test", password, roleId = 2 };
    await Request(HttpMethod.Post, "/api/v1/users", 401, userBody);
    await Request(HttpMethod.Post, "/api/v1/users", 403, userBody, employeeToken);
    await Request(HttpMethod.Post, "/api/v1/users", 403, userBody, Token(role: "Administrador"));
    await Request(HttpMethod.Post, "/api/v1/users", 201, userBody, adminToken);
    await Request(HttpMethod.Get, "/api/v1/users", 403, token: employeeToken);
    await Request(HttpMethod.Get, $"/api/v1/users/{admin.Id}", 403, token: employeeToken);
    await Request(HttpMethod.Get, "/api/v1/roles", 403, token: employeeToken);
    await Request(HttpMethod.Get, "/api/v1/roles", 200, token: adminToken);
    await Request(HttpMethod.Get, "/api/v1/users", 200, token: adminToken);

    // Cambio del estado persistido simulado, sin tocar MySQL ni el modelo de producción.
    typeof(User).GetProperty(nameof(User.RoleId))!.SetValue(admin, 2);
    await Request(HttpMethod.Post, "/api/v1/users", 403, userBody, adminToken);
    var changed = await Request(HttpMethod.Get, "/api/v1/auth/me", 200, token: adminToken);
    Check(changed.GetProperty("role").GetString() == "Empleado", "No se aplicó el nuevo rol.");
    typeof(User).GetProperty(nameof(User.RoleId))!.SetValue(admin, 1);
    var otherToken = (await authentication.LoginAsync(new(other.Email.Value, password)))!.AccessToken;
    other.Deactivate();
    await Request(HttpMethod.Get, "/api/v1/auth/me", 401, token: otherToken);

    object Items(int quantity) => new[] { new { productId = product.Id, quantity, unitAmount = 3m } };
    await Request(HttpMethod.Post, "/api/v1/purchases", 401, new { items = Items(20) });
    await Request(HttpMethod.Post, "/api/v1/sales", 401, new { items = Items(1) });
    await Request(HttpMethod.Post, "/api/v1/purchases", 400, new { userId = admin.Id, items = Items(20) }, employeeToken);
    await Request(HttpMethod.Post, "/api/v1/sales", 400, new { userId = admin.Id, items = Items(1) }, employeeToken);
    try { CommercialIdentity.Creating(new(admin.Id, "Admin", admin.Email.Value, 1, "Administrador"), employee.Id); throw new Exception("Suplantación aceptada."); }
    catch (CommercialConfirmationException e) { Check(e.Failure == ConfirmationFailure.Forbidden, "Suplantación no devolvió Forbidden."); }

    var purchase = await Request(HttpMethod.Post, "/api/v1/purchases", 201, new { items = Items(20) }, employeeToken);
    var purchaseId = purchase.GetProperty("id").GetGuid();
    Check(purchase.GetProperty("userId").GetGuid() == employee.Id, "La compra no conserva el usuario autenticado.");
    await Request(HttpMethod.Post, $"/api/v1/purchases/{purchaseId}/confirm", 401);
    await Request(HttpMethod.Post, $"/api/v1/purchases/{purchaseId}/confirm", 200, token: employeeToken);
    Check(product.Stock.Value == 20 && store.Movements.Last().UserId == employee.Id, "Compra o trazabilidad incorrecta.");
    await Request(HttpMethod.Post, $"/api/v1/purchases/{purchaseId}/confirm", 409, token: employeeToken);
    var sale = await Request(HttpMethod.Post, "/api/v1/sales", 201, new { items = Items(3) }, employeeToken);
    var saleId = sale.GetProperty("id").GetGuid();
    await Request(HttpMethod.Post, $"/api/v1/sales/{saleId}/confirm", 401);
    await Request(HttpMethod.Post, $"/api/v1/sales/{saleId}/confirm", 200, token: adminToken);
    Check(product.Stock.Value == 17 && store.Movements.Last().UserId == admin.Id
        && store.Sales.Single(s => s.Id == saleId).UserId == employee.Id, "Administrador alteró creador o actor del movimiento.");
    await Request(HttpMethod.Post, $"/api/v1/sales/{saleId}/confirm", 409, token: employeeToken);
    var foreignPurchase = new BodeGo.API.Commercial.Domain.Model.Aggregates.Purchase(admin.Id);
    foreignPurchase.AddItem(product.Id, null, new(1), new(3)); store.Purchases.Add(foreignPurchase);
    var foreignSale = new BodeGo.API.Commercial.Domain.Model.Aggregates.Sale(admin.Id);
    foreignSale.AddItem(product.Id, null, new(1), new(3)); store.Sales.Add(foreignSale);
    var before = (product.Stock.Value, product.Version, store.Movements.Count);
    await Request(HttpMethod.Post, $"/api/v1/purchases/{foreignPurchase.Id}/confirm", 403, token: employeeToken);
    await Request(HttpMethod.Post, $"/api/v1/sales/{foreignSale.Id}/confirm", 403, token: employeeToken);
    Check(before == (product.Stock.Value, product.Version, store.Movements.Count), "Una confirmación denegada cambió inventario.");
    await Request(HttpMethod.Post, $"/api/v1/purchases/{foreignPurchase.Id}/confirm", 200, token: adminToken);
    await Request(HttpMethod.Post, $"/api/v1/sales/{foreignSale.Id}/confirm", 200, token: adminToken);

    var openapi = await http.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
    var operations = openapi.GetProperty("paths").EnumerateObject()
        .Sum(p => p.Value.EnumerateObject().Count(v => new[] { "get", "post", "put", "patch", "delete" }.Contains(v.Name)));
    Check(operations == 32, "OpenAPI no contiene 32 operaciones.");
    var originalRoutes = new Dictionary<string, string[]>
    {
        ["/api/v1/users"] = ["get", "post"], ["/api/v1/users/{id}"] = ["get"],
        ["/api/v1/roles"] = ["get"], ["/api/v1/roles/{id}"] = ["get"],
        ["/api/v1/categories"] = ["get", "post"], ["/api/v1/categories/{id}"] = ["get"],
        ["/api/v1/products"] = ["get", "post"], ["/api/v1/products/{id}"] = ["get"],
        ["/api/v1/products/{id}/lots"] = ["get"], ["/api/v1/products/low-stock"] = ["get"],
        ["/api/v1/purchases"] = ["get", "post"], ["/api/v1/purchases/{id}"] = ["get"],
        ["/api/v1/purchases/{id}/confirm"] = ["post"],
        ["/api/v1/sales"] = ["get", "post"], ["/api/v1/sales/{id}"] = ["get"],
        ["/api/v1/sales/{id}/confirm"] = ["post"],
        ["/api/v1/wastes"] = ["get", "post"], ["/api/v1/inventory-movements"] = ["get"],
        ["/api/v1/inventory-counts"] = ["post"], ["/api/v1/inventory-counts/{id}"] = ["get"],
        ["/api/v1/alerts"] = ["get"], ["/api/v1/alerts/{id}"] = ["get"],
        ["/api/v1/reports/sales-summary"] = ["get"], ["/api/v1/reports/top-products"] = ["get"]
    };
    Check(originalRoutes.Sum(r => r.Value.Length) == 30 && originalRoutes.All(route =>
        openapi.GetProperty("paths").TryGetProperty(route.Key, out var path)
        && route.Value.All(method => path.TryGetProperty(method, out _))),
        "No se conservaron las 30 rutas originales.");
    Check(openapi.GetProperty("paths").GetProperty("/api/v1/auth/login").TryGetProperty("post", out _)
        && openapi.GetProperty("paths").GetProperty("/api/v1/auth/me").TryGetProperty("get", out _),
        "Faltan las dos rutas de autenticación.");
    Check(openapi.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString() == "bearer", "Falta Bearer en OpenAPI.");
    foreach (var (path, method) in new[] { ("/api/v1/users", "post"), ("/api/v1/users", "get"), ("/api/v1/roles", "get"), ("/api/v1/auth/me", "get"), ("/api/v1/purchases", "post"), ("/api/v1/sales/{id}/confirm", "post") })
        Check(openapi.GetProperty("paths").GetProperty(path).GetProperty(method).GetProperty("security")[0].TryGetProperty("Bearer", out _), "Falta seguridad OpenAPI.");
    using var scalar = await http.GetAsync("/scalar");
    Check(scalar.IsSuccessStatusCode && (await scalar.Content.ReadAsStringAsync()).Contains("BodeGo API"), "Scalar no está disponible.");
    using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/v1/purchases");
    preflight.Headers.Add("Origin", "http://localhost:5174");
    preflight.Headers.Add("Access-Control-Request-Method", "POST");
    preflight.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
    using var preflightResult = await http.SendAsync(preflight);
    Check(preflightResult.IsSuccessStatusCode && preflightResult.Headers.GetValues("Access-Control-Allow-Origin").Single() == "http://localhost:5174", "Se rompió CORS Bearer.");
    Console.WriteLine($"OK: {checks} comprobaciones de autenticación, autorización, Commercial, CORS, Scalar y OpenAPI. HTTP/JWT reales; persistencia con dobles, sin MySQL.");
}
finally { await app.StopAsync(); }
if (args is ["--mysql"]) await MySqlAuthenticationChecks.Run();
else if (args.Length != 0) throw new ArgumentException("Uso: --inspect-mysql (sólo lectura) o --mysql (base aislada nueva).");
