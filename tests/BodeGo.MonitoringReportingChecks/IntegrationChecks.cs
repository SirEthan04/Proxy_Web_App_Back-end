using System.Net;
using System.Text.Json;
using BodeGo.API.Commercial.Domain.Model.ValueObjects;
using BodeGo.API.Monitoring.Infrastructure.Persistence.EFC.Repositories;
using BodeGo.API.Reporting.Application.Model.Queries;
using BodeGo.API.Reporting.Infrastructure.Persistence.EFC.Queries;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

internal static class IntegrationChecks
{
    public static async Task Run(string baseUrl)
    {
        var checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks++;
        }
        try
        {
            var uri = new Uri(baseUrl);
            Check(uri.IsLoopback, "Solo se permite una API local.");
            using var db = new BodeGoDbContextFactory().CreateDbContext([]);
            var connection = new MySqlConnectionStringBuilder(db.Database.GetConnectionString());
            Check(connection.Server is "localhost" or "127.0.0.1" && connection.Port == 3306 &&
                connection.Database == "bodego_db", "Solo se permite bodego_db local.");
            Check(await db.Database.CanConnectAsync(), "MySQL no está disponible.");
            Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "Hay migraciones existentes pendientes.");
            var stockBefore = await db.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
            var snapshot = stockBefore.Select(p => (p.Id, p.Stock.Value, p.Version)).ToArray();
            var sales = await db.Sales.AsNoTracking().ToListAsync();
            var items = await db.SaleItems.AsNoTracking().ToListAsync();
            var products = await db.Products.AsNoTracking().ToListAsync();
            var alerts = await new AlertRepository(db).ListAsync();
            using var http = new HttpClient { BaseAddress = uri };
            async Task<JsonElement> Get(string path, HttpStatusCode expected = HttpStatusCode.OK)
            {
                using var response = await http.GetAsync(path);
                Check(response.StatusCode == expected, $"Estado HTTP incorrecto: {path}.");
                var text = await response.Content.ReadAsStringAsync();
                if (!path.StartsWith("/openapi/", StringComparison.Ordinal))
                    Check(!text.Contains("password", StringComparison.OrdinalIgnoreCase), "Respuesta expone credenciales.");
                return JsonDocument.Parse(text).RootElement.Clone();
            }
            var alertList = await Get("/api/v1/alerts");
            Check(alertList.GetArrayLength() == alerts.Count, "Lista HTTP distinta de MySQL.");
            Check(alertList.EnumerateArray().Select(a => a.GetProperty("id").GetGuid()).SequenceEqual(alerts.Select(a => a.Id)), "Orden/IDs de alertas distintos.");
            if (alerts.Count > 0)
            {
                var found = await Get($"/api/v1/alerts/{alerts[0].Id}");
                Check(found.GetProperty("message").GetString() == alerts[0].Message, "La alerta no coincide con MySQL.");
            }
            else Console.WriteLine("MySQL: alerts vacía; GET devuelve []. Alerta existente probada solo con doble unitario.");
            var missing = Guid.NewGuid();
            while (alerts.Any(a => a.Id == missing)) missing = Guid.NewGuid();
            var notFound = await Get($"/api/v1/alerts/{missing}", HttpStatusCode.NotFound);
            Check(notFound.GetProperty("title").GetString() == "No se encontró la alerta solicitada.", "404 no traducido.");
            foreach (var path in new[] {
                "/api/v1/alerts/invalid", "/api/v1/alerts/00000000-0000-0000-0000-000000000000",
                "/api/v1/reports/sales-summary?from=2026-11-11&to=2026-11-10",
                "/api/v1/reports/sales-summary?from=10-11-2026",
                "/api/v1/reports/top-products?limit=0", "/api/v1/reports/top-products?limit=101",
                "/api/v1/reports/top-products?limit=abc", "/api/v1/reports/top-products?limit=",
                "/api/v1/reports/top-products?to=2026-02-30" })
            {
                var problem = await Get(path, HttpStatusCode.BadRequest);
                Check(!string.IsNullOrEmpty(problem.GetProperty("title").GetString()), "400 sin mensaje.");
            }
            foreach (var (suffix, from, to) in new[] {
                ("", (DateOnly?)null, (DateOnly?)null),
                ("?from=2026-11-10&to=2026-11-10", (DateOnly?)new DateOnly(2026, 11, 10), (DateOnly?)new DateOnly(2026, 11, 10)) })
            {
                var range = new ReportDateRange(from, to);
                var included = sales.Where(s => s.Status == CommercialStatus.Confirmed &&
                    (!range.StartUtc.HasValue || s.RegisteredAt >= range.StartUtc) &&
                    (!range.EndExclusiveUtc.HasValue || s.RegisteredAt < range.EndExclusiveUtc)).ToArray();
                var summary = await Get("/api/v1/reports/sales-summary" + suffix);
                Check(summary.GetProperty("salesCount").GetInt64() == included.LongLength, "Cantidad no coincide con ventas reales.");
                Check(summary.GetProperty("totalAmount").GetDecimal() == included.Sum(s => s.Total.Value), "Importe no coincide con ventas reales.");
                var ids = included.Select(s => s.Id).ToHashSet();
                var expected = items.Where(i => ids.Contains(i.SaleId)).GroupBy(i => i.ProductId)
                    .Select(g => new { Id = g.Key, Quantity = g.Sum(i => (long)i.Quantity.Value) })
                    .OrderByDescending(p => p.Quantity).ThenBy(p => p.Id.ToString(), StringComparer.Ordinal).Take(10).ToArray();
                var top = await Get("/api/v1/reports/top-products" + suffix);
                Check(top.GetArrayLength() == expected.Length, "Ranking real tiene otra cantidad de productos.");
                for (var i = 0; i < expected.Length; i++)
                {
                    Check(top[i].GetProperty("productId").GetGuid() == expected[i].Id &&
                        top[i].GetProperty("quantitySold").GetInt64() == expected[i].Quantity &&
                        top[i].GetProperty("productName").GetString() == products.Single(p => p.Id == expected[i].Id).Name.Value,
                        "Ranking real incorrecto.");
                }
            }
            var openapi = await Get("/openapi/v1.json");
            var paths = openapi.GetProperty("paths");
            foreach (var (path, tag) in new[] { ("/api/v1/alerts", "Monitoring"), ("/api/v1/alerts/{id}", "Monitoring"),
                ("/api/v1/reports/sales-summary", "Reporting"), ("/api/v1/reports/top-products", "Reporting") })
            {
                var operation = paths.GetProperty(path).GetProperty("get");
                Check(operation.GetProperty("tags").EnumerateArray().Any(t => t.GetString() == tag), "Etiqueta OpenAPI incorrecta.");
                Check(operation.GetProperty("responses").TryGetProperty("200", out _), "Falta contrato 200.");
            }
            Check(paths.TryGetProperty("/api/v1/users", out _) && paths.TryGetProperty("/api/v1/products", out _), "Otros módulos no aparecen en OpenAPI.");
            var stockAfter = await db.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
            Check(snapshot.SequenceEqual(stockAfter.Select(p => (p.Id, p.Stock.Value, p.Version))), "El inventario cambió durante las consultas.");
            Check(db.ChangeTracker.Entries().Count() == 0, "Las consultas rastrean entidades.");
            Console.WriteLine($"OK: {checks} verificaciones HTTP/MySQL con datos reales; {sales.Count(s => s.Status == CommercialStatus.Confirmed)} ventas confirmadas existentes. Sin escrituras.");

            // SQL real del repositorio sobre CTE virtuales: nunca persisten ventas de prueba.
            using var fixtureDb = new BodeGoDbContext(new DbContextOptionsBuilder<BodeGoDbContext>()
                .UseMySQL(connection.ConnectionString).AddInterceptors(new ReadOnlyFixtureInterceptor()).Options);
            var fixtureSummary = new SalesSummaryRepository(fixtureDb);
            var fixtureTop = new TopProductsRepository(fixtureDb);
            var sameDay = new ReportDateRange(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 10));
            var summaryResult = await fixtureSummary.GetAsync(sameDay);
            Check(summaryResult.SalesCount == 2 && summaryResult.TotalAmount == 19.50m, "SQL no excluye Draft/Cancelled o falla en límites/importes.");
            var topResult = await fixtureTop.GetAsync(sameDay, 10);
            Check(topResult.Count == 2 && topResult[0].ProductName == "Azucar unitaria" && topResult[0].QuantitySold == 7 &&
                topResult[1].QuantitySold == 5, "SQL no agrupa ni ordena las cantidades correctamente.");
            Check((await fixtureTop.GetAsync(sameDay, 1)).Count == 1, "SQL no limita resultados.");
            var emptyRange = new ReportDateRange(new DateOnly(2000, 1, 1), new DateOnly(2000, 1, 1));
            var emptySummary = await fixtureSummary.GetAsync(emptyRange);
            Check(emptySummary.SalesCount == 0 && emptySummary.TotalAmount == 0 && (await fixtureTop.GetAsync(emptyRange, 10)).Count == 0,
                "SQL no devuelve ceros/lista vacía cuando faltan ventas.");
            Console.WriteLine($"OK: {checks} verificaciones acumuladas, incluidas fixtures SQL virtuales explícitas. No son ventas reales ni se persistieron.");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Falló la integración ({exception.GetType().Name}) después de {checks} verificaciones; no se afirma aprobación.");
            if (exception.StackTrace?.Contains("g__Check", StringComparison.Ordinal) == true)
                Console.Error.WriteLine(exception.Message);
            Environment.ExitCode = 1;
        }
    }
}
