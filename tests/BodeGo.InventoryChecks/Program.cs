using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BodeGo.API.Inventory.Application.Internal.CommandServices;
using BodeGo.API.Inventory.Application.Internal.QueryServices;
using BodeGo.API.Inventory.Domain.Exceptions;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.Commands;
using BodeGo.API.Inventory.Domain.Model.Entities;
using BodeGo.API.Inventory.Domain.Model.Queries;
using BodeGo.API.Inventory.Domain.Model.ValueObjects;
using BodeGo.API.Inventory.Domain.Repositories;
using BodeGo.API.Inventory.Infrastructure;
using BodeGo.API.Inventory.Infrastructure.Persistence.EFC.Repositories;
using BodeGo.API.Inventory.Interfaces.REST;
using BodeGo.API.Inventory.Interfaces.REST.Resources;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++;
}
async Task Reject<T>(Func<Task> action) where T : Exception
{
    try { await action(); }
    catch (T exception) { Check(!string.IsNullOrWhiteSpace(exception.Message), "Falta mensaje de error."); return; }
    throw new InvalidOperationException($"Se esperaba {typeof(T).Name}.");
}
Task Invalid(Action action) => Reject<ArgumentException>(() => { action(); return Task.CompletedTask; });
foreach (var name in new[] { "", " ", new string('a', 101) })
    await Invalid(() => new Category(name));
await Invalid(() => new Category("Válida", " "));
await Invalid(() => new Category("Válida", new string('a', 256)));
Check(new Category(" " + new string('a', 100) + " ", new string('b', 255)).Name.Length == 100, "Límites de categoría.");
foreach (var name in new[] { "", " ", new string('a', 151) }) await Invalid(() => new ProductName(name));
Check(new ProductName(new string('a', 150)).Value.Length == 150, "Límite del nombre.");
foreach (var price in new[] { -1m, 0.001m, 10000000000m }) await Invalid(() => new Money(price));
Check(new Money(0).Value == 0 && new Money(9999999999.99m).Value == 9999999999.99m, "Límites de precio.");
await Invalid(() => new StockLevel(-1));
await Invalid(() => new Product(Guid.Empty, new ProductName("Arroz"), new Money(1), new StockLevel(0)));

var memory = new MemoryRepositories();
var categoryCommands = new CategoryCommandService(memory);
var categoryQueries = new CategoryQueryService(memory);
var productCommands = new ProductCommandService(memory, memory);
var productQueries = new ProductQueryService(memory);
Check((await categoryQueries.Handle(new GetCategoriesQuery())).Count == 0, "Categorías vacías.");
Check((await productQueries.Handle(new GetProductsQuery())).Count == 0, "Productos vacíos.");
var category = await categoryCommands.Handle(new CreateCategoryCommand(" Abarrotes ", " Productos secos "));
Check(category.Name == "Abarrotes" && category.Description == "Productos secos" && category.Active, "Registro de categoría.");
await Reject<DuplicateCategoryException>(() => categoryCommands.Handle(new CreateCategoryCommand("abarrotes", null)));
Check(memory.Categories.Count == 1, "El duplicado fue insertado.");
await Reject<ArgumentException>(() => categoryCommands.Handle(new CreateCategoryCommand(" ", null)));
Check((await categoryQueries.Handle(new GetCategoryByIdQuery(category.Id))) == category, "Buscar categoría.");
Check(await categoryQueries.Handle(new GetCategoryByIdQuery(Guid.NewGuid())) is null, "Categoría inexistente.");
await Reject<ArgumentException>(() => categoryQueries.Handle(new GetCategoryByIdQuery(Guid.Empty)));
var created = await productCommands.Handle(new CreateProductCommand(category.Id, " Arroz ", 3.50m, 2));
Check(created.Name == "Arroz" && created.Stock == 0 && created.Version == 0 && created.Active, "Producto sin stock ficticio.");
Check(memory.Products.Single().Lots.Count == 0, "Se crearon lotes ficticios.");
await Reject<CategoryNotFoundException>(() => productCommands.Handle(new CreateProductCommand(Guid.NewGuid(), "Otro", 1, 0)));
await Reject<ArgumentException>(() => productCommands.Handle(new CreateProductCommand(category.Id, "Otro", -1, 0)));
await Reject<ArgumentException>(() => productCommands.Handle(new CreateProductCommand(category.Id, "Otro", 1, -1)));
await Reject<ArgumentException>(() => productCommands.Handle(new CreateProductCommand(category.Id, "Otro", 1.001m, 0)));
Check(memory.Products.Count == 1, "Se insertó un producto inválido.");
// No unique product-name restriction exists: identical names are allowed.
await productCommands.Handle(new CreateProductCommand(category.Id, "Arroz", 0, 0));
Check(memory.Products.Count == 2, "Se impuso una unicidad inexistente.");
Check((await productQueries.Handle(new GetProductByIdQuery(created.Id))) == created, "Buscar producto.");
Check(await productQueries.Handle(new GetProductByIdQuery(Guid.NewGuid())) is null, "Producto inexistente.");
Check((await productQueries.Handle(new GetProductLotsQuery(created.Id)))!.Count == 0, "Producto sin lotes.");
Check(await productQueries.Handle(new GetProductLotsQuery(Guid.NewGuid())) is null, "Lotes de producto inexistente.");
await Reject<ArgumentException>(() => productQueries.Handle(new GetProductByIdQuery(Guid.Empty)));
await Reject<ArgumentException>(() => productQueries.Handle(new GetProductLotsQuery(Guid.Empty)));
Check((await productQueries.Handle(new GetLowStockProductsQuery())).Count == 2, "Stock cero y mínimo cero deben incluirse.");

// Domain-only fixture. No persistence or inventory movement is claimed.
var stocked = memory.Products[0];
var lot = stocked.ReceiveLot("LOTE-UNITARIO", new DateOnly(2027, 12, 31), 2);
Check((await productQueries.Handle(new GetLowStockProductsQuery())).Any(p => p.Id == stocked.Id), "Stock igual al mínimo.");
Check((await productQueries.Handle(new GetProductLotsQuery(stocked.Id)))!.Single().QuantityAvailable == 2, "Proyección del lote.");
stocked.ReceiveLot("LOTE-UNITARIO-2", null, 1);
Check(!(await productQueries.Handle(new GetLowStockProductsQuery())).Any(p => p.Id == stocked.Id), "Stock sobre el mínimo.");
stocked.RemoveStock(lot.Id, 1);
Check((await productQueries.Handle(new GetLowStockProductsQuery())).Any(p => p.Id == stocked.Id), "Stock vuelve al mínimo.");
Check((await productQueries.Handle(new GetProductsQuery())).Count == 2, "Listado de productos.");

// Controllers are invoked directly with application services and explicit test doubles.
var categoriesController = new CategoriesController(categoryCommands, categoryQueries)
    { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
var productsController = new ProductsController(productCommands, productQueries)
    { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
Check(await categoriesController.Create(new CreateCategoryResource { Name = "Bebidas" }, default) is CreatedAtActionResult { StatusCode: 201, ActionName: "GetById" }, "POST categoría 201.");
Check(await categoriesController.Create(new CreateCategoryResource { Name = "Bebidas" }, default) is ObjectResult { StatusCode: 409 }, "POST categoría 409.");
Check(await categoriesController.Create(new CreateCategoryResource { Name = " " }, default) is ObjectResult { StatusCode: 400 }, "POST categoría 400.");
Check(await categoriesController.GetById(Guid.NewGuid(), default) is ObjectResult { StatusCode: 404 }, "GET categoría 404.");
Check(await categoriesController.GetById(category.Id, default) is OkObjectResult, "GET categoría 200.");
Check(await categoriesController.GetAll(default) is OkObjectResult, "GET categorías 200.");
Check(await productsController.Create(new CreateProductResource { CategoryId = category.Id, Name = "Azúcar", SalePrice = 2, MinimumStock = 1 }, default)
    is CreatedAtActionResult { StatusCode: 201, ActionName: "GetById" }, "POST producto 201.");
Check(await productsController.Create(new CreateProductResource { CategoryId = Guid.NewGuid(), Name = "Azúcar", SalePrice = 2, MinimumStock = 1 }, default)
    is ObjectResult { StatusCode: 400 }, "POST producto categoría inexistente.");
Check(await productsController.GetById(Guid.NewGuid(), default) is ObjectResult { StatusCode: 404 }, "GET producto 404.");
Check(await productsController.GetById(created.Id, default) is OkObjectResult, "GET producto 200.");
Check(await productsController.GetLots(Guid.NewGuid(), default) is ObjectResult { StatusCode: 404 }, "GET lotes 404.");
Check(await productsController.GetLots(memory.Products.Last().Id, default) is OkObjectResult { Value: IEnumerable<ProductLotResource> empty } && !empty.Any(), "GET lotes vacíos 200.");
Check(await productsController.GetById(Guid.Empty, default) is ObjectResult { StatusCode: 400 }, "GUID vacío 400.");
Check(await productsController.GetAll(default) is OkObjectResult && await productsController.GetLowStock(default) is OkObjectResult, "Listados 200.");

var validation = new List<ValidationResult>();
var missingFields = new CreateProductResource();
Check(!Validator.TryValidateObject(missingFields, new ValidationContext(missingFields), validation, true)
    && validation.Count == 4, "Campos numéricos omitidos deben rechazarse.");

// Metadata and SQL generation with the actual MySQL provider, without opening a connection.
using var db = new BodeGoDbContext(new DbContextOptionsBuilder<BodeGoDbContext>()
    .UseMySQL("Server=localhost;Database=bodego_db;User=root;").Options);
var sql = ProductRepository.LowStockQuery(db).ToQueryString();
Check(sql.Contains("stock <= minimum_stock"), "SQL de stock bajo.");
Check(db.Database.GetDbConnection().State == System.Data.ConnectionState.Closed, "Se abrió una conexión.");
var services = new ServiceCollection();
services.AddLogging();
services.AddControllers().AddApplicationPart(typeof(ProductsController).Assembly);
services.AddInventory();
services.AddScoped(_ => db);
using (var provider = services.BuildServiceProvider())
using (var scope = provider.CreateScope())
{
    Check(scope.ServiceProvider.GetRequiredService<BodeGo.API.Inventory.Application.Services.IProductQueryService>() is ProductQueryService, "Registro de servicios.");
}
Console.WriteLine($"OK: {checks} comprobaciones de Inventory sin MySQL. No son pruebas de integración.");

// Optional: verify only generated documentation on a running API. Never calls a DB endpoint.
if (args is [ "--openapi", var baseUrl ])
{
    using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    var document = await http.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
    var paths = document.GetProperty("paths");
    var expected = new[] {
        ("/api/v1/categories", "post"), ("/api/v1/categories", "get"),
        ("/api/v1/categories/{id}", "get"), ("/api/v1/products", "post"),
        ("/api/v1/products", "get"), ("/api/v1/products/{id}", "get"),
        ("/api/v1/products/{id}/lots", "get"), ("/api/v1/products/low-stock", "get")
    };
    foreach (var (path, method) in expected)
        Check(paths.GetProperty(path).GetProperty(method).GetProperty("tags").EnumerateArray().Any(t => t.GetString() == "Inventory"), $"Falta {method} {path} en Inventory.");
    var operations = paths.EnumerateObject().SelectMany(p => p.Value.EnumerateObject())
        .Where(p => new[] { "get", "post", "put", "patch", "delete" }.Contains(p.Name)).ToArray();
    Check(operations.Count(p => p.Value.GetProperty("tags").EnumerateArray().Any(t => t.GetString() == "Inventory")) == 8, "Conteo Inventory.");
    Check(operations.Count(p => p.Value.GetProperty("tags").EnumerateArray().Any(t => t.GetString() == "IAM")) == 5, "Conteo IAM.");
    Console.WriteLine("OK: OpenAPI documenta 8 endpoints Inventory y conserva 5 IAM. HTTP/MySQL: PENDIENTE.");
}

sealed class MemoryRepositories : ICategoryRepository, IProductRepository
{
    public List<Category> Categories { get; } = [];
    public List<Product> Products { get; } = [];
    Task<Category?> ICategoryRepository.FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Categories.SingleOrDefault(c => c.Id == id));
    Task<IReadOnlyList<Category>> ICategoryRepository.ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Category>>(Categories.ToArray());
    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(Categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
    public Task AddAsync(Category category, CancellationToken cancellationToken = default) { Categories.Add(category); return Task.CompletedTask; }
    public Task<Product?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Products.SingleOrDefault(p => p.Id == id));
    public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Product>>(Products.ToArray());
    public Task<IReadOnlyList<Product>> ListLowStockAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Product>>(Products.Where(p => p.Stock.Value <= p.MinimumStock.Value).ToArray());
    public Task<IReadOnlyList<ProductLot>> ListLotsAsync(Guid productId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProductLot>>(Products.Where(p => p.Id == productId).SelectMany(p => p.Lots).ToArray());
    public Task AddAsync(Product product, CancellationToken cancellationToken = default) { Products.Add(product); return Task.CompletedTask; }
}
