using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.ValueObjects;
using BodeGo.API.Operations.Application.Internal.CommandServices;
using BodeGo.API.Operations.Application.Internal.QueryServices;
using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain.Exceptions;
using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Commands;
using BodeGo.API.Operations.Domain.Model.Entities;
using BodeGo.API.Operations.Domain.Model.Queries;
using BodeGo.API.Operations.Domain.Repositories;
using BodeGo.API.Operations.Infrastructure;
using BodeGo.API.Operations.Interfaces.REST;
using BodeGo.API.Operations.Interfaces.REST.Resources;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
async Task Reject(Func<Task> action, bool conflict = false)
{
    try { await action(); }
    catch (OperationsException e) { Check(e.Conflict == conflict, e.Message); return; }
    throw new Exception("Se esperaba un error de Operations.");
}
var memory = new MemorySession();
var product = memory.Product;
var lot = product.Lots.First();
var commands = new WasteCommandService(memory, memory);
CreateWasteCommand Waste(int quantity = 1, Guid? lotId = null) =>
    new(product.Id, lotId, memory.UserId, quantity, "Daño");
foreach (var quantity in new[] { 0, -1 })
    await Reject(() => commands.Handle(Waste(quantity)));
await Reject(() => commands.Handle(Waste() with { ProductId = Guid.NewGuid() }));
await Reject(() => commands.Handle(Waste() with { UserId = Guid.NewGuid() }));
await Reject(() => commands.Handle(Waste(lotId: Guid.NewGuid())));
await Reject(() => commands.Handle(Waste() with { Reason = " " }));
await Reject(() => commands.Handle(Waste() with { Reason = new string('x', 256) }));
await Reject(() => commands.Handle(Waste(11)), true);
await Reject(() => commands.Handle(Waste(7, lot.Id)), true);
Check(product.Stock.Value == 10 && memory.Wastes.Count == 0, "Las validaciones cambiaron stock.");
var version = product.Version;
var waste = await commands.Handle(Waste(3, lot.Id));
Check(product.Stock.Value == 7 && lot.QuantityAvailable == 3 && product.Version == version + 1, "Descuento por lote.");
Check(memory.Movements.Single() is { QuantityDelta: -3, SourceType: "Waste" } movement &&
    movement.SourceId == waste.Id.ToString() && movement.LotId == lot.Id, "Origen del movimiento.");
var second = await commands.Handle(Waste(5));
Check(product.Stock.Value == 2 && memory.Movements.Where(x => x.SourceId == second.Id.ToString()).Sum(x => x.QuantityDelta) == -5,
    "Descuento distribuido entre lotes.");
Check(memory.Movements.Count == 3, "Se espera un movimiento por lote utilizado.");
Check((await new WasteQueryService(memory).Handle(new GetWastesQuery())).Count == 2, "Consulta de mermas.");
Check((await new InventoryMovementQueryService(memory).Handle(new GetInventoryMovementsQuery())).Count == 3, "Consulta de movimientos.");

var counts = new InventoryCountCommandService(memory, memory);
CreateInventoryCountCommand Count(params CountItemCommand[] items) => new(memory.UserId, "Control físico", items);
var item = new CountItemCommand(product.Id, null, 1);
await Reject(() => counts.Handle(Count()));
await Reject(() => counts.Handle(Count(item with { CountedQuantity = -1 })));
await Reject(() => counts.Handle(Count(item with { ProductId = Guid.NewGuid() })));
await Reject(() => counts.Handle(Count(item with { LotId = Guid.NewGuid() })));
await Reject(() => counts.Handle(Count(item, item)));
await Reject(() => counts.Handle(Count(item, item with { LotId = lot.Id })));
await Reject(() => counts.Handle(Count(item) with { Notes = " " }));
var count = await counts.Handle(Count(item));
Check(count.Items.Single() is { ExpectedQuantity: 2, CountedQuantity: 1, Difference: -1 }, "Fotografía del conteo.");
Check(product.Stock.Value == 2 && memory.Movements.Count == 3, "Conteo modificó el inventario.");
Check(await new InventoryCountQueryService(memory).Handle(new GetInventoryCountByIdQuery(count.Id)) == count, "Consulta con detalles.");
Check(await new InventoryCountQueryService(memory).Handle(new GetInventoryCountByIdQuery(Guid.NewGuid())) == null, "Consulta inexistente.");
Check(memory.WritesInsideSession, "Las escrituras deben estar dentro de la unidad de trabajo.");

// El proveedor real genera SQL y configura concurrencia sin abrir MySQL.
using (var db = new BodeGoDbContext(new DbContextOptionsBuilder<BodeGoDbContext>()
    .UseMySQL("Server=localhost;Database=bodego_db;User=root;").Options))
{
    Check(db.Model.FindEntityType(typeof(Product))!.FindProperty(nameof(Product.Version))!.IsConcurrencyToken, "Falta token de concurrencia.");
    Check(db.InventoryCounts.AsNoTracking().Include(x => x.Items).ToQueryString().Contains("inventory_count_items"), "Faltan detalles SQL.");
    Check(db.Database.GetDbConnection().State == System.Data.ConnectionState.Closed, "Se abrió MySQL.");
}

// HTTP real con Kestrel y dobles de los puertos; no demuestra transacciones MySQL.
var builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddControllers().AddApplicationPart(typeof(WastesController).Assembly);
builder.Services.AddOpenApi();
builder.Services.AddOperations();
builder.Services.AddSingleton<IOperationsRepository>(memory);
builder.Services.AddSingleton<IOperationsSession>(memory);
await using var app = builder.Build();
app.MapControllers();
app.MapOpenApi();
await app.StartAsync();
try
{
    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    using var http = new HttpClient { BaseAddress = new Uri(address) };
    async Task Status(HttpResponseMessage response, HttpStatusCode expected)
    {
        using (response) Check(response.StatusCode == expected,
            $"{response.RequestMessage?.RequestUri}: esperado {expected}, recibido {response.StatusCode}. {await response.Content.ReadAsStringAsync()}");
    }
    await Status(await http.GetAsync("/api/v1/inventory-movements"), HttpStatusCode.OK);
    await Status(await http.GetAsync("/api/v1/wastes"), HttpStatusCode.OK);
    await Status(await http.GetAsync($"/api/v1/inventory-counts/{count.Id}"), HttpStatusCode.OK);
    await Status(await http.GetAsync($"/api/v1/inventory-counts/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
    await Status(await http.GetAsync("/api/v1/inventory-counts/no-es-guid"), HttpStatusCode.BadRequest);
    await Status(await http.PostAsJsonAsync("/api/v1/wastes", new { }), HttpStatusCode.BadRequest);
    await Status(await http.PostAsync("/api/v1/wastes", new StringContent("{", Encoding.UTF8, "application/json")), HttpStatusCode.BadRequest);
    await Status(await http.PostAsJsonAsync("/api/v1/wastes", new CreateWasteResource(product.Id, null, memory.UserId, 3, "Pérdida")), HttpStatusCode.Conflict);
    await Status(await http.PostAsJsonAsync("/api/v1/wastes", new CreateWasteResource(product.Id, null, memory.UserId, 1, "Vencimiento")), HttpStatusCode.Created);
    await Status(await http.PostAsJsonAsync("/api/v1/inventory-counts",
        new CreateInventoryCountResource(memory.UserId, null, [new(product.Id, null, 0)])), HttpStatusCode.Created);
    await Status(await http.PostAsJsonAsync("/api/v1/inventory-counts",
        new { userId = memory.UserId, items = new object?[] { null } }), HttpStatusCode.BadRequest);
    await Status(await http.PostAsJsonAsync("/api/v1/inventory-counts",
        new { userId = memory.UserId, items = new[] { new { productId = product.Id } } }), HttpStatusCode.BadRequest);
    var doc = await http.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
    foreach (var (path, method) in new[] {
        ("/api/v1/inventory-movements", "get"), ("/api/v1/wastes", "get"), ("/api/v1/wastes", "post"),
        ("/api/v1/inventory-counts", "post"), ("/api/v1/inventory-counts/{id}", "get") })
        Check(doc.GetProperty("paths").GetProperty(path).GetProperty(method).GetProperty("tags")
            .EnumerateArray().Any(x => x.GetString() == "Operations"), $"Falta etiqueta de {method} {path}.");
}
finally { await app.StopAsync(); }
Console.WriteLine($"OK: {checks} comprobaciones de Operations. HTTP con dobles; MySQL, rollback y concurrencia real PENDIENTES.");
if (args.Contains("--mysql")) await MySqlChecks.Run();

sealed class MemorySession : IOperationsRepository, IOperationsSession
{
    public Guid UserId { get; } = Guid.NewGuid();
    public Product Product { get; } = new(Guid.NewGuid(), new ProductName("Arroz"), new Money(3), new StockLevel(1));
    public List<Waste> Wastes { get; } = [];
    public List<InventoryMovement> Movements { get; } = [];
    public List<InventoryCount> Counts { get; } = [];
    public bool WritesInsideSession { get; private set; } = true;
    private bool inside;
    public MemorySession() { Product.ReceiveLot("A", null, 6); Product.ReceiveLot("B", null, 4); }
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        inside = true;
        try { return await operation(); }
        finally { inside = false; }
    }
    public Task<Product?> LoadProductAsync(Guid id, CancellationToken ct) => Task.FromResult(id == Product.Id ? Product : null);
    public Task<bool> IsActiveUserAsync(Guid id, CancellationToken ct) => Task.FromResult(id == UserId);
    public void Add(Waste waste, IReadOnlyList<InventoryMovement> movements)
    { WritesInsideSession &= inside; Wastes.Add(waste); Movements.AddRange(movements); }
    public void Add(InventoryCount count) { WritesInsideSession &= inside; Counts.Add(count); }
    public Task<IReadOnlyList<Waste>> ListWastesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Waste>>(Wastes.ToArray());
    public Task<IReadOnlyList<InventoryMovement>> ListMovementsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<InventoryMovement>>(Movements.ToArray());
    public Task<InventoryCount?> FindCountAsync(Guid id, CancellationToken ct) => Task.FromResult(Counts.SingleOrDefault(x => x.Id == id));
}
