using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using BodeGo.API.Commercial.Infrastructure;
using BodeGo.API.IAM.Domain.Model.Aggregates;
using BodeGo.API.IAM.Domain.Model.Entities;
using BodeGo.API.IAM.Domain.Model.ValueObjects;
using BodeGo.API.IAM.Infrastructure;
using BodeGo.API.IAM.Infrastructure.Authentication;
using BodeGo.API.IAM.Infrastructure.Hashing;
using BodeGo.API.IAM.Interfaces.REST;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.Entities;
using BodeGo.API.Inventory.Domain.Model.ValueObjects;
using BodeGo.API.Inventory.Infrastructure;
using BodeGo.API.Monitoring.Infrastructure;
using BodeGo.API.Operations.Infrastructure;
using BodeGo.API.Reporting.Infrastructure;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using Scalar.AspNetCore;

internal static class MySqlAuthenticationChecks
{
    public static async Task InspectExisting()
    {
        try
        {
            using var configured = new BodeGoDbContextFactory().CreateDbContext([]);
            var settings = new MySqlConnectionStringBuilder(configured.Database.GetConnectionString());
            if (settings.Server is not ("localhost" or "127.0.0.1"))
                throw new InvalidOperationException("Sólo se permite MySQL local.");
            settings.Database = "";
            await using var connection = new MySqlConnection(settings.ConnectionString);
            await connection.OpenAsync();
            var databases = new List<string>();
            await using (var command = new MySqlCommand(
                "SELECT schema_name FROM information_schema.schemata WHERE LOCATE(@prefix, schema_name) = 1 ORDER BY schema_name", connection))
            {
                command.Parameters.AddWithValue("@prefix", "bodego_test_autenticacion_");
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) databases.Add(reader.GetString(0));
            }
            if (databases.Count == 0) Console.WriteLine("No existen bases bodego_test_autenticacion_*. No se creó ninguna.");
            foreach (var database in databases)
            {
                if (!database.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
                    throw new InvalidOperationException("Nombre de base no permitido.");
                var tables = new List<string>();
                await using (var command = new MySqlCommand(
                    "SELECT table_name FROM information_schema.tables WHERE table_schema = @database ORDER BY table_name", connection))
                {
                    command.Parameters.AddWithValue("@database", database);
                    await using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
                }
                Console.WriteLine(database + ": " + tables.Count + " tablas.");
                foreach (var table in new[] { "__EFMigrationsHistory", "users", "products", "purchases", "sales", "inventory_movements" }
                    .Where(t => tables.Contains(t, StringComparer.OrdinalIgnoreCase)))
                {
                    await using var count = new MySqlCommand("SELECT COUNT(*) FROM `" + database + "`.`" + table + "`", connection);
                    Console.WriteLine("  " + table + ": " + await count.ExecuteScalarAsync() + " filas.");
                }
            }
            Console.WriteLine("Inspección terminada: sólo SELECT, sin migraciones ni escrituras.");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("No se pudieron inspeccionar las bases aisladas (" + exception.GetType().Name + "). Sin escrituras.");
            Environment.ExitCode = 1;
        }
    }

    public static async Task Run()
    {
        var checks = 0;
        void Check(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException(message);
            checks++;
        }
        try
        {
            using var configured = new BodeGoDbContextFactory().CreateDbContext([]);
            var settings = new MySqlConnectionStringBuilder(configured.Database.GetConnectionString());
            Check(settings.Server is "localhost" or "127.0.0.1", "Sólo se permite MySQL local.");
            var database = "bodego_test_autenticacion_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "_" + Guid.NewGuid().ToString("N")[..8];
            settings.Database = "";
            await using (var server = new MySqlConnection(settings.ConnectionString))
            {
                await server.OpenAsync();
                // Nombre generado internamente; nunca proviene del usuario o de la base habitual.
                await using var command = new MySqlCommand("CREATE DATABASE `" + database + "` CHARACTER SET utf8mb4", server);
                await command.ExecuteNonQueryAsync();
            }
            settings.Database = database;
            var isolated = settings.ConnectionString;
            Check(settings.Database.StartsWith("bodego_test_autenticacion_", StringComparison.Ordinal), "Base no aislada.");
            Console.WriteLine("Base aislada creada: " + database + ". No se borrará al finalizar.");
            DbContextOptions<BodeGoDbContext> Options() => new DbContextOptionsBuilder<BodeGoDbContext>().UseMySQL(isolated).Options;
            var hasher = new PasswordHashService();
            var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
            var admin = new User(1, "Administrador de pruebas aisladas", new EmailAddress("admin@example.test"), hasher.Hash(password));
            var employee = new User(2, "Empleado de pruebas aisladas", new EmailAddress("employee@example.test"), hasher.Hash(password));
            var other = new User(2, "Otro empleado", new EmailAddress("other@example.test"), hasher.Hash(password));
            var inactive = new User(2, "Inactivo", new EmailAddress("inactive@example.test"), hasher.Hash(password));
            inactive.Deactivate();
            var category = new Category("Categoría JWT");
            var product = new Product(category.Id, new ProductName("Producto JWT MySQL"), new Money(3), new StockLevel(0));
            await using (var seed = new BodeGoDbContext(Options()))
            {
                await seed.Database.MigrateAsync();
                seed.Roles.AddRange(new Role(1, "Administrador"), new Role(2, "Empleado"));
                seed.Users.AddRange(admin, employee, other, inactive);
                seed.Categories.Add(category); seed.Products.Add(product);
                await seed.SaveChangesAsync();
                Check(await seed.Users.CountAsync(u => u.RoleId == 1 && u.Active) == 1
                    && hasher.Verify((await seed.Users.SingleAsync(u => u.Id == admin.Id)).PasswordHash, password),
                    "El administrador de prueba no quedó registrado de forma segura.");
            }
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var jwtSettings = new JwtSettings
            { SigningKeyBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)) };
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKeyBase64"] = jwtSettings.SigningKeyBase64,
                ["Jwt:Issuer"] = jwtSettings.Issuer, ["Jwt:Audience"] = jwtSettings.Audience
            });
            builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
            builder.Services.AddOpenApi(IamOpenApi.Configure);
            builder.Services.AddIam(); builder.Services.AddIamAuthentication(builder.Configuration);
            builder.Services.AddCommercial(); builder.Services.AddInventory(); builder.Services.AddOperations();
            builder.Services.AddMonitoring(); builder.Services.AddReporting();
            builder.Services.AddDbContext<BodeGoDbContext>(o => o.UseMySQL(isolated));
            builder.Services.AddCors(options => options.AddPolicy("BodeGoFrontend",
                p => p.WithOrigins("http://localhost:5174").AllowAnyMethod().AllowAnyHeader()));
            await using var app = builder.Build();
            app.UseRouting(); app.UseCors("BodeGoFrontend"); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
            app.MapControllers(); app.MapOpenApi(); app.MapScalarApiReference(o => o.WithTitle("BodeGo API"));
            await app.StartAsync();
            try
            {
                var url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                using var http = new HttpClient { BaseAddress = new Uri(url) };
                async Task<JsonElement> Request(HttpMethod method, string path, int expected, object? body = null, string? token = null)
                {
                    using var message = new HttpRequestMessage(method, path);
                    if (token != null) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    if (body != null) message.Content = JsonContent.Create(body);
                    using var response = await http.SendAsync(message);
                    Check((int)response.StatusCode == expected, path + ": HTTP " + (int)response.StatusCode + ", esperado " + expected);
                    var text = await response.Content.ReadAsStringAsync();
                    Check(!text.Contains(password) && !text.Contains("passwordHash", StringComparison.OrdinalIgnoreCase)
                        && !text.Contains("password_hash", StringComparison.OrdinalIgnoreCase), "Respuesta expone contraseña/hash.");
                    return JsonDocument.Parse(text).RootElement.Clone();
                }
                var adminLogin = await Request(HttpMethod.Post, "/api/v1/auth/login", 200, new { email = admin.Email.Value, password });
                var adminToken = adminLogin.GetProperty("accessToken").GetString()!;
                var login = await Request(HttpMethod.Post, "/api/v1/auth/login", 200, new { email = " EMPLOYEE@EXAMPLE.TEST ", password });
                var token = login.GetProperty("accessToken").GetString()!;
                await Request(HttpMethod.Post, "/api/v1/auth/login", 401, new { email = "missing@example.test", password });
                await Request(HttpMethod.Post, "/api/v1/auth/login", 401, new { email = employee.Email.Value, password = "incorrecta" });
                await Request(HttpMethod.Post, "/api/v1/auth/login", 401, new { email = inactive.Email.Value, password });
                await Request(HttpMethod.Get, "/api/v1/auth/me", 200, token: token);
                await Request(HttpMethod.Get, "/api/v1/auth/me", 401);
                string InvalidToken(string issuer, string audience, bool expired = false)
                {
                    var now = DateTime.UtcNow;
                    return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer, audience,
                        [new Claim("sub", employee.Id.ToString())],
                        expired ? now.AddHours(-2) : now,
                        expired ? now.AddHours(-1) : now.AddMinutes(5),
                        new SigningCredentials(jwtSettings.Key(), SecurityAlgorithms.HmacSha256)));
                }
                await Request(HttpMethod.Get, "/api/v1/auth/me", 401,
                    token: InvalidToken(jwtSettings.Issuer, jwtSettings.Audience, expired: true));
                await Request(HttpMethod.Get, "/api/v1/auth/me", 401,
                    token: InvalidToken("emisor-incorrecto", jwtSettings.Audience));
                await Request(HttpMethod.Get, "/api/v1/auth/me", 401,
                    token: InvalidToken(jwtSettings.Issuer, "audiencia-incorrecta"));
                var altered = token.Split('.');
                altered[2] = (altered[2][0] == 'a' ? "b" : "a") + altered[2][1..];
                await Request(HttpMethod.Get, "/api/v1/auth/me", 401, token: string.Join('.', altered));
                await Request(HttpMethod.Post, "/api/v1/auth/login", 429, new { email = employee.Email.Value, password });
                foreach (var origin in new[] { "http://localhost:5174", "http://origen-no-autorizado.example" })
                {
                    using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/v1/purchases");
                    preflight.Headers.Add("Origin", origin);
                    preflight.Headers.Add("Access-Control-Request-Method", "POST");
                    preflight.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
                    using var response = await http.SendAsync(preflight);
                    if (origin == "http://localhost:5174")
                        Check(response.IsSuccessStatusCode
                            && response.Headers.GetValues("Access-Control-Allow-Origin").Single() == origin
                            && response.Headers.GetValues("Access-Control-Allow-Headers").Single().Contains("authorization", StringComparison.OrdinalIgnoreCase),
                            "CORS no permite Authorization desde Vue.");
                    else Check(!response.Headers.Contains("Access-Control-Allow-Origin"), "CORS permite un origen ajeno.");
                }
                var newUser = new { name = "Alta JWT", email = "new@example.test", password, roleId = 2 };
                await Request(HttpMethod.Post, "/api/v1/users", 403, newUser, token);
                await Request(HttpMethod.Post, "/api/v1/users", 403,
                    new { name = "Intento administrador", email = "not-created@example.test", password, roleId = 1 }, token);
                var registered = await Request(HttpMethod.Post, "/api/v1/users", 201, newUser, adminToken);
                await Request(HttpMethod.Post, "/api/v1/users", 409, newUser, adminToken);
                await using (var db = new BodeGoDbContext(Options()))
                {
                    var user = await db.Users.SingleAsync(u => u.Id == registered.GetProperty("id").GetGuid());
                    Check(hasher.Verify(user.PasswordHash, password) && user.PasswordHash != password, "No se persistió un hash verificable.");
                    db.Entry(user).Property(u => u.RoleId).CurrentValue = 1;
                    await db.SaveChangesAsync();
                }
                // Se modifica el rol sólo del usuario ficticio recién creado en la base aislada.
                var temporaryToken = app.Services.CreateScope();
                using (temporaryToken)
                {
                    var issuer = temporaryToken.ServiceProvider.GetRequiredService<BodeGo.API.IAM.Application.Services.IAccessTokenService>();
                    var newId = registered.GetProperty("id").GetGuid();
                    var newToken = issuer.Issue(new(newId, "Alta JWT", "new@example.test", 2, "Empleado")).AccessToken;
                    await Request(HttpMethod.Get, "/api/v1/users", 200, token: newToken);
                    await using var db = new BodeGoDbContext(Options());
                    var user = await db.Users.SingleAsync(u => u.Id == newId);
                    db.Entry(user).Property(u => u.RoleId).CurrentValue = 2;
                    await db.SaveChangesAsync();
                    await Request(HttpMethod.Get, "/api/v1/users", 403, token: newToken);
                    user.Deactivate(); await db.SaveChangesAsync();
                    await Request(HttpMethod.Get, "/api/v1/auth/me", 401, token: newToken);
                }
                object Items(int quantity) => new[] { new { productId = product.Id, quantity, unitAmount = 3m } };
                await Request(HttpMethod.Post, "/api/v1/purchases", 400, new { userId = admin.Id, items = Items(20) }, token);
                await Request(HttpMethod.Post, "/api/v1/sales", 400, new { userId = admin.Id, items = Items(1) }, token);
                var purchase = await Request(HttpMethod.Post, "/api/v1/purchases", 201, new { items = Items(20) }, token);
                var purchaseId = purchase.GetProperty("id").GetGuid();
                await Request(HttpMethod.Post, $"/api/v1/purchases/{purchaseId}/confirm", 200, token: token);
                await Request(HttpMethod.Post, $"/api/v1/purchases/{purchaseId}/confirm", 409, token: token);
                var sale = await Request(HttpMethod.Post, "/api/v1/sales", 201, new { items = Items(3) }, token);
                var saleId = sale.GetProperty("id").GetGuid();
                await Request(HttpMethod.Post, $"/api/v1/sales/{saleId}/confirm", 200, token: adminToken);
                await using (var db = new BodeGoDbContext(Options()))
                {
                    Check((await db.Products.SingleAsync(p => p.Id == product.Id)).Stock.Value == 17, "Stock no quedó en 17.");
                    Check((await db.Sales.SingleAsync(s => s.Id == saleId)).UserId == employee.Id, "Se cambió el creador.");
                    Check((await db.InventoryMovements.SingleAsync(m => m.SourceId == saleId.ToString())).UserId == admin.Id, "Movimiento no identifica al confirmador.");
                }
                var foreign = await Request(HttpMethod.Post, "/api/v1/purchases", 201, new { items = Items(1) }, adminToken);
                await Request(HttpMethod.Post, $"/api/v1/purchases/{foreign.GetProperty("id").GetGuid()}/confirm", 403, token: token);
                var foreignSale = await Request(HttpMethod.Post, "/api/v1/sales", 201, new { items = Items(1) }, adminToken);
                await Request(HttpMethod.Post, $"/api/v1/sales/{foreignSale.GetProperty("id").GetGuid()}/confirm", 403, token: token);
                await using (var db = new BodeGoDbContext(Options()))
                {
                    Check((await db.Products.SingleAsync(p => p.Id == product.Id)).Stock.Value == 17
                        && await db.InventoryMovements.CountAsync(m => m.ProductId == product.Id) == 2,
                        "Una confirmación denegada cambió stock o movimientos.");
                }
                var summary = await Request(HttpMethod.Get, "/api/v1/reports/sales-summary", 200);
                Check(summary.GetProperty("salesCount").GetInt64() == 1 && summary.GetProperty("totalAmount").GetDecimal() == 9, "Reporting no refleja la venta confirmada.");
                var previousConnection = Environment.GetEnvironmentVariable("BODEGO_TEST_MYSQL_CONNECTION");
                var previousToken = Environment.GetEnvironmentVariable("BODEGO_TEST_ACCESS_TOKEN");
                try
                {
                    Environment.SetEnvironmentVariable("BODEGO_TEST_MYSQL_CONNECTION", isolated);
                    Environment.SetEnvironmentVariable("BODEGO_TEST_ACCESS_TOKEN", adminToken);
                    await CommercialIntegrationChecks.Run(url);
                    Check(Environment.ExitCode == 0, "Falló la suite de transacciones comerciales aisladas.");
                }
                finally
                {
                    Environment.SetEnvironmentVariable("BODEGO_TEST_MYSQL_CONNECTION", previousConnection);
                    Environment.SetEnvironmentVariable("BODEGO_TEST_ACCESS_TOKEN", previousToken);
                }
                Console.WriteLine($"OK: {checks} comprobaciones HTTP/MySQL de autenticación y autorización en {database}. Sin escrituras en bodego_db.");
            }
            finally { await app.StopAsync(); }
        }
        catch (Exception exception)
        {
            // No imprimir excepciones SQL, cadenas de conexión, tokens ni contraseñas.
            Console.Error.WriteLine($"Pruebas MySQL aisladas fallidas ({exception.GetType().Name}); {checks} comprobaciones completadas.");
            if (exception.StackTrace?.Contains("g__Check", StringComparison.Ordinal) == true) Console.Error.WriteLine(exception.Message);
            Environment.ExitCode = 1;
        }
    }
}
