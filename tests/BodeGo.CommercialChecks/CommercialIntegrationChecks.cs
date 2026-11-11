using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BodeGo.API.Commercial.Application.Internal.CommandServices;
using BodeGo.API.Commercial.Application.Services;
using BodeGo.API.Commercial.Domain.Exceptions;
using BodeGo.API.Commercial.Domain.Model.Commands;
using BodeGo.API.Commercial.Domain.Model.ValueObjects;
using BodeGo.API.Commercial.Infrastructure.Persistence.EFC;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

internal static class CommercialIntegrationChecks
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
            Check(new Uri(baseUrl).IsLoopback, "Solo se permite una API local.");
            var isolatedConnection = Environment.GetEnvironmentVariable("BODEGO_TEST_MYSQL_CONNECTION")
                ?? throw new InvalidOperationException("Configure una base aislada mediante BODEGO_TEST_MYSQL_CONNECTION.");
            var token = Environment.GetEnvironmentVariable("BODEGO_TEST_ACCESS_TOKEN")
                ?? throw new InvalidOperationException("Configure un JWT válido de la API aislada.");
            using var configured = new BodeGoDbContext(new DbContextOptionsBuilder<BodeGoDbContext>().UseMySQL(isolatedConnection).Options);
            var connection = new MySqlConnectionStringBuilder(configured.Database.GetConnectionString());
            Check(connection.Server is "localhost" or "127.0.0.1" && connection.Database.StartsWith("bodego_test_", StringComparison.Ordinal),
                "Solo se permite una base local aislada bodego_test_*.");
            Check(await configured.Database.CanConnectAsync(), "MySQL no está disponible.");
            Check(!(await configured.Database.GetPendingMigrationsAsync()).Any(), "Aplicar primero la migración común.");
            var engineCount = await configured.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name IN ('products','product_lots','purchases','purchase_items','sales','sale_items','inventory_movements') AND engine = 'InnoDB'").SingleAsync();
            Check(engineCount == 7, "Las tablas transaccionales requieren InnoDB.");
            BodeGoDbContext NewDb(bool fail = false)
            {
                var options = new DbContextOptionsBuilder<BodeGoDbContext>().UseMySQL(connection.ConnectionString);
                if (fail) options.AddInterceptors(new FailAfterConfirmationSave());
                return new BodeGoDbContext(options.Options);
            }
            using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(45) };
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var identity = await http.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
            var userId = identity.GetProperty("id").GetGuid();
            Check(await configured.Users.AnyAsync(u => u.Id == userId && u.Active), "La identidad no pertenece a la base aislada.");
            var actor = new CheckCurrentUser(userId);
            async Task<JsonElement> Request(HttpMethod method, string path, object? body, int expected)
            {
                using var message = new HttpRequestMessage(method, path);
                if (body != null) message.Content = JsonContent.Create(body);
                using var response = await http.SendAsync(message);
                Check((int)response.StatusCode == expected, $"HTTP inesperado en {method} {path}: {(int)response.StatusCode}.");
                var text = await response.Content.ReadAsStringAsync();
                Check(!text.Contains("MySqlException", StringComparison.OrdinalIgnoreCase) && !text.Contains("password_hash", StringComparison.OrdinalIgnoreCase), "La respuesta expone detalles internos.");
                return JsonDocument.Parse(text).RootElement.Clone();
            }
            Task<JsonElement> Get(string path) => Request(HttpMethod.Get, path, null, 200);
            Task<JsonElement> Confirm(string kind, Guid id, int status = 200) => Request(HttpMethod.Post, $"/api/v1/{kind}/{id}/confirm", null, status);
            var suffix = Guid.NewGuid().ToString("N");
            var category = await Request(HttpMethod.Post, "/api/v1/categories", new { name = "Pruebas confirmaciones " + suffix, description = "Datos de desarrollo identificables; no borrar historia" }, 201);
            var categoryId = category.GetProperty("id").GetGuid();
            async Task<Guid> Product(string name)
            {
                var resource = await Request(HttpMethod.Post, "/api/v1/products", new { categoryId, name = "Prueba confirmación " + name + " " + suffix, salePrice = 2.5m, minimumStock = 0 }, 201);
                return resource.GetProperty("id").GetGuid();
            }
            object Item(Guid productId, int quantity, Guid? lotId = null) => new { productId, lotId, quantity, unitAmount = 2.5m };
            async Task<Guid> Draft(string kind, params object[] items)
            {
                var resource = await Request(HttpMethod.Post, "/api/v1/" + kind, new { items }, 201);
                Check(resource.GetProperty("status").GetString() == "Draft", "La creación ya no genera Draft.");
                return resource.GetProperty("id").GetGuid();
            }
            async Task<(int Stock, int Version, int Lots, long Sum, int Moves)> Snapshot(Guid productId)
            {
                using var db = NewDb();
                var p = await db.Products.AsNoTracking().Include(p => p.Lots).SingleAsync(p => p.Id == productId);
                return (p.Stock.Value, p.Version, p.Lots.Count, p.Lots.Sum(l => (long)l.QuantityAvailable),
                    await db.InventoryMovements.CountAsync(m => m.ProductId == productId));
            }
            async Task Movement(Guid sourceId, int delta, string kind)
            {
                using var db = NewDb();
                var rows = await db.InventoryMovements.AsNoTracking().Where(m => m.SourceId == sourceId.ToString()).ToArrayAsync();
                Check(rows.Length > 0 && rows.Sum(m => m.QuantityDelta) == delta && rows.All(m => m.SourceType == kind && m.MovementType.ToString() == kind && m.LotId.HasValue), "Movimientos incoherentes.");
            }
            var first = await Product("flujo 0-20-17");
            Check((await Snapshot(first)).Stock == 0, "Producto inicial no tiene stock cero.");
            var purchaseId = await Draft("purchases", Item(first, 20));
            var received = await Confirm("purchases", purchaseId);
            var lotId = received.GetProperty("items")[0].GetProperty("lotId").GetGuid();
            Check(received.GetProperty("status").GetString() == "Confirmed", "Compra no confirmada.");
            var stock20 = await Snapshot(first);
            Check(stock20.Stock == 20 && stock20.Sum == 20 && stock20.Version == 1, "Compra no ingresó stock y lote.");
            await Movement(purchaseId, 20, "Purchase");
            await Confirm("purchases", purchaseId, 409);
            Check(await Snapshot(first) == stock20, "Doble compra modificó inventario.");
            var reportBefore = await Get("/api/v1/reports/sales-summary");
            var saleId = await Draft("sales", Item(first, 3, lotId));
            await Confirm("sales", saleId);
            var stock17 = await Snapshot(first);
            Check(stock17.Stock == 17 && stock17.Sum == 17 && stock17.Version == 2, "Venta no dejó stock 17.");
            await Movement(saleId, -3, "Sale");
            await Confirm("sales", saleId, 409);
            Check(await Snapshot(first) == stock17, "Doble venta modificó inventario.");
            var reportAfter = await Get("/api/v1/reports/sales-summary");
            Check(reportAfter.GetProperty("salesCount").GetInt64() == reportBefore.GetProperty("salesCount").GetInt64() + 1 &&
                reportAfter.GetProperty("totalAmount").GetDecimal() == reportBefore.GetProperty("totalAmount").GetDecimal() + 7.5m, "Reporting no incluyó la venta confirmada.");
            var excessive = await Draft("sales", Item(first, 18, lotId));
            await Confirm("sales", excessive, 409);
            Check(await Snapshot(first) == stock17, "Stock insuficiente alteró inventario.");
            Check((await Get($"/api/v1/sales/{excessive}")).GetProperty("status").GetString() == "Draft", "Stock insuficiente cambió el estado.");
            foreach (var kind in new[] { "purchases", "sales" })
            {
                await Request(HttpMethod.Post, $"/api/v1/{kind}/invalid/confirm", null, 400);
                await Request(HttpMethod.Post, $"/api/v1/{kind}/{Guid.Empty}/confirm", null, 400);
                await Confirm(kind, Guid.NewGuid(), 404);
            }
            var second = await Product("varios detalles");
            var multiPurchase = await Draft("purchases", Item(first, 2, lotId), Item(first, 3, lotId), Item(second, 7));
            await Confirm("purchases", multiPurchase);
            Check((await Snapshot(first)).Stock == 22 && (await Snapshot(second)).Stock == 7, "Compra multidetalle no sumó productos.");
            await Movement(multiPurchase, 12, "Purchase");
            var multiSale = await Draft("sales", Item(first, 1, lotId), Item(second, 2), Item(first, 2));
            await Confirm("sales", multiSale);
            Check((await Snapshot(first)).Stock == 19 && (await Snapshot(second)).Stock == 5, "Venta multidetalle no descontó productos.");
            await Movement(multiSale, -5, "Sale");
            await Request(HttpMethod.Post, "/api/v1/sales", new { items = new[] { Item(second, 1, lotId) } }, 400);

            async Task Rollback(bool isPurchase)
            {
                var kind = isPurchase ? "purchases" : "sales";
                var id = await Draft(kind, Item(second, 1));
                var before = await Snapshot(second);
                using (var failing = NewDb(fail: true))
                {
                    var session = new CommercialConfirmationSession(failing);
                    try
                    {
                        if (isPurchase) await new PurchaseConfirmationService(session, actor).Handle(new ConfirmPurchaseCommand(id));
                        else await new SaleConfirmationService(session, actor).Handle(new ConfirmSaleCommand(id));
                        throw new InvalidOperationException("No se produjo el fallo inyectado.");
                    }
                    catch (InjectedConfirmationFailure) { checks++; }
                }
                Check(await Snapshot(second) == before, "Rollback posterior a SaveChanges falló en inventario/movimientos.");
                var resource = await Get($"/api/v1/{kind}/{id}");
                Check(resource.GetProperty("status").GetString() == "Draft", "Rollback no restauró Draft.");
                if (isPurchase) Check(resource.GetProperty("items")[0].GetProperty("lotId").ValueKind == JsonValueKind.Null, "Rollback no restauró el detalle.");
            }
            await Rollback(true);
            await Rollback(false);

            async Task HeaderConcurrency(bool isPurchase)
            {
                var kind = isPurchase ? "purchases" : "sales";
                var id = await Draft(kind, Item(second, 1));
                var before = await Snapshot(second);
                var acquired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var firstDb = NewDb();
                using var secondDb = NewDb();
                var gated = new GateAfterHeaderLock(new CommercialConfirmationSession(firstDb), isPurchase, async () =>
                { acquired.SetResult(); await release.Task.WaitAsync(TimeSpan.FromSeconds(20)); });
                async Task<bool> Execute(ICommercialConfirmationSession session)
                {
                    try
                    {
                        if (isPurchase) await new PurchaseConfirmationService(session, actor).Handle(new ConfirmPurchaseCommand(id));
                        else await new SaleConfirmationService(session, actor).Handle(new ConfirmSaleCommand(id));
                        return true;
                    }
                    catch (CommercialConfirmationException e) when (e.Failure == ConfirmationFailure.Conflict) { return false; }
                }
                var one = Execute(gated);
                await acquired.Task.WaitAsync(TimeSpan.FromSeconds(20));
                var two = Execute(new CommercialConfirmationSession(secondDb));
                try { await Task.Delay(200); Check(!two.IsCompleted, "La segunda confirmación no esperó el bloqueo de cabecera."); }
                finally { release.TrySetResult(); }
                var results = await Task.WhenAll(one, two).WaitAsync(TimeSpan.FromSeconds(30));
                Check(results.Count(x => x) == 1, "La misma operación se confirmó dos veces.");
                var after = await Snapshot(second);
                Check(after.Stock == before.Stock + (isPurchase ? 1 : -1) && after.Moves == before.Moves + 1, "Concurrencia duplicó efectos.");
                await Movement(id, isPurchase ? 1 : -1, isPurchase ? "Purchase" : "Sale");
            }
            await HeaderConcurrency(true);
            await HeaderConcurrency(false);
            var concurrentPurchase = await Draft("purchases", Item(second, 1));
            async Task<int> PostStatus(Guid id, string kind)
            {
                using var response = await http.PostAsync($"/api/v1/{kind}/{id}/confirm", null);
                return (int)response.StatusCode;
            }
            var sameHttp = await Task.WhenAll(PostStatus(concurrentPurchase, "purchases"), PostStatus(concurrentPurchase, "purchases"));
            Check(sameHttp.Order().SequenceEqual(new[] { 200, 409 }), "HTTP simultáneo de compra no devolvió un éxito y un conflicto.");
            var available = (await Snapshot(second)).Stock;
            var competing1 = await Draft("sales", Item(second, available));
            var competing2 = await Draft("sales", Item(second, available));
            var competing = await Task.WhenAll(PostStatus(competing1, "sales"), PostStatus(competing2, "sales"));
            Check(competing.Order().SequenceEqual(new[] { 200, 409 }), "Dos ventas distintas sobrevendieron el mismo stock.");
            Check((await Snapshot(second)).Stock == 0 && (await Snapshot(second)).Sum == 0, "Concurrencia produjo stock negativo/incoherente.");
            var rollbackBefore = await Snapshot(first);
            var invalidMulti = await Draft("sales", Item(first, 1), Item(second, 1));
            await Confirm("sales", invalidMulti, 409);
            Check(await Snapshot(first) == rollbackBefore, "Venta multidetalle insuficiente confirmó parcialmente.");
            var api = (await Get("/openapi/v1.json")).GetProperty("paths");
            var operations = api.EnumerateObject().Sum(path => path.Value.EnumerateObject().Count(op => op.Name is "get" or "post" or "put" or "delete" or "patch"));
            Check(operations == 32, "OpenAPI no contiene exactamente 32 endpoints.");
            foreach (var kind in new[] { "purchases", "sales" })
            {
                var operation = api.GetProperty($"/api/v1/{kind}/{{id}}/confirm").GetProperty("post");
                Check(operation.GetProperty("tags").EnumerateArray().Any(t => t.GetString() == "Commercial"), "Etiqueta Commercial ausente.");
            }
            Check(configured.Model.FindEntityType(typeof(BodeGo.API.Inventory.Domain.Model.Aggregates.Product))!.FindProperty("Version")!.IsConcurrencyToken,
                "Se perdió el token Version.");
            Console.WriteLine($"OK: {checks} verificaciones HTTP/MySQL reales: flujo 0→20→17, lotes, movimientos, duplicados, rollback después de SaveChanges, bloqueos y concurrencia.");
            Console.WriteLine($"Datos nuevos identificados por {suffix}. Productos para Workbench: {first}, {second}. No se borraron datos ni se modificaron productos previos.");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Integración fallida ({exception.GetType().Name}) después de {checks} comprobaciones. No se afirma aprobación.");
            if (exception.StackTrace?.Contains("g__Check", StringComparison.Ordinal) == true) Console.Error.WriteLine(exception.Message);
            Environment.ExitCode = 1;
        }
    }
}
