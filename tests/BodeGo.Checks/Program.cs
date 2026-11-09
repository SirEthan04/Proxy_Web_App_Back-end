using BodeGo.API.Commercial.Domain.Model.Aggregates;
using BodeGo.API.Commercial.Domain.Model.ValueObjects;
using BodeGo.API.IAM.Domain.Model.ValueObjects;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.ValueObjects;
using BodeGo.API.Monitoring.Domain.Model.Aggregates;
using BodeGo.API.Monitoring.Domain.Model.ValueObjects;
using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Entities;
using BodeGo.API.Operations.Domain.Model.ValueObjects;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using CommercialMoney = BodeGo.API.Commercial.Domain.Model.ValueObjects.Money;
using InventoryMoney = BodeGo.API.Inventory.Domain.Model.ValueObjects.Money;

var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
void Reject(Action action)
{
    try { action(); }
    catch (Exception e) when (e is ArgumentException or InvalidOperationException or OverflowException)
    { checks++; return; }
    throw new Exception("An invalid operation was accepted.");
}

var userId = Guid.NewGuid();
var product = new Product(Guid.NewGuid(), new ProductName("Rice"), new InventoryMoney(3.50m), new StockLevel(2));
var lot = product.ReceiveLot("LOT-1", new DateOnly(2027, 12, 31), 10);
product.RemoveStock(lot.Id, 3);
Check(product.Stock.Value == 7 && lot.QuantityAvailable == 7 && product.Version == 2, "Stock and lot diverged.");
Reject(() => product.RemoveStock(lot.Id, 8));
Reject(() => product.RemoveStock(Guid.NewGuid(), 1));
Reject(() => product.ReceiveLot("lot-1", null, 1));
Check(product.Stock.Value == 7 && product.Version == 2, "Rejected stock operation changed the aggregate.");
Reject(() => new StockLevel(-1));
Reject(() => new ProductName(" "));
Reject(() => new InventoryMoney(-1));
Reject(() => new CommercialMoney(1.001m));
Reject(() => new Quantity(0));
Reject(() => new EmailAddress("invalid"));
Check(new EmailAddress(" Person@Example.com ").Value == "person@example.com", "Email normalization failed.");

var purchase = new Purchase(userId);
purchase.AddItem(product.Id, lot.Id, new Quantity(3), new CommercialMoney(1.25m));
purchase.AddItem(product.Id, lot.Id, new Quantity(2), new CommercialMoney(2));
Check(purchase.Total.Value == 7.75m && purchase.Items.All(i => i.PurchaseId == purchase.Id), "Purchase total or ownership failed.");
purchase.Confirm();
Reject(() => purchase.AddItem(product.Id, null, new Quantity(1), new CommercialMoney(1)));
Reject(purchase.Cancel);
var sale = new Sale(userId);
Reject(sale.Confirm);
sale.AddItem(product.Id, lot.Id, new Quantity(2), new CommercialMoney(3.50m));
Check(sale.Total.Value == 7 && sale.Items.Single().Subtotal.Value == 7, "Sale total failed.");
sale.Confirm();
Reject(() => sale.AddItem(product.Id, null, new Quantity(1), new CommercialMoney(1)));
var count = new InventoryCount(userId);
count.AddItem(product.Id, lot.Id, 7, 5);
Check(count.Items.Single().Difference == -2, "Count difference failed.");
Reject(() => count.AddItem(product.Id, lot.Id, 7, 5));
Reject(() => count.AddItem(product.Id, null, 7, 5));
count.Confirm();
Reject(() => count.AddItem(Guid.NewGuid(), null, 0, 0));
Reject(() => new Waste(product.Id, lot.Id, userId, 0, "Broken"));
Reject(() => new InventoryMovement(product.Id, lot.Id, userId, MovementType.Sale, 1, "Sale", sale.Id));
Reject(() => new InventoryMovement(product.Id, lot.Id, userId, MovementType.Purchase, 0, "Purchase", purchase.Id));
Reject(() => new Alert(product.Id, null, AlertType.Expiration, "Expired"));
var alert = new Alert(product.Id, lot.Id, AlertType.Expiration, "Expires soon");
alert.Attend(userId);
Check(alert.Status == AlertStatus.Attended && alert.AttendedAt != null && alert.AttendedByUserId == userId, "Alert attendance failed.");
Reject(() => alert.Attend(userId));

// No connection is opened: validate metadata, generated SQL and change tracking.
using var db = new BodeGoDbContext(new DbContextOptionsBuilder<BodeGoDbContext>()
    .UseMySQL("Server=localhost;Database=bodego_db;User=root;").Options);
var model = db.GetService<IDesignTimeModel>().Model;
var tables = model.GetEntityTypes().ToArray();
var expectedTables = new[] { "roles", "users", "categories", "products", "product_lots", "purchases",
    "purchase_items", "sales", "sale_items", "inventory_movements", "wastes", "inventory_counts", "inventory_count_items", "alerts" };
Check(tables.Select(t => t.GetTableName()).Order().SequenceEqual(expectedTables.Order()), "Unexpected business tables.");
Check(tables.All(t => t.FindPrimaryKey() != null), "Missing primary key.");
var foreignKeys = tables.SelectMany(t => t.GetForeignKeys()).ToArray();
Check(foreignKeys.Length == 24 && foreignKeys.All(k => k.DeleteBehavior == DeleteBehavior.Restrict), "Foreign key count or deletion rules failed.");
Check(foreignKeys.Count(k => k.Properties.Count == 2 && k.PrincipalEntityType.ClrType.Name == "ProductLot") == 6, "Product/lot consistency is missing.");
foreach (var property in tables.SelectMany(t => t.GetProperties()).Where(p =>
             (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType) == typeof(Guid)))
    Check(property.GetColumnType() == "varchar(36)" && property.GetValueConverter()?.ProviderClrType == typeof(string), "Inconsistent Guid storage.");
Check(model.FindEntityType(typeof(Product))!.FindProperty("Version")!.IsConcurrencyToken, "Concurrency token missing.");
Check(tables.SelectMany(t => t.GetProperties()).All(p => !p.IsShadowProperty()), "An unintended shadow column was created.");
var migration = db.GetService<IMigrationsAssembly>().CreateMigration(
    db.GetService<IMigrationsAssembly>().Migrations.Single().Value, db.Database.ProviderName!);
Check(migration.UpOperations.OfType<CreateTableOperation>().Count() == 14, "Migration does not create 14 tables.");
Check(migration.UpOperations.All(o => o is CreateTableOperation or CreateIndexOperation or AlterDatabaseOperation), "Unexpected migration operation.");
var sql = db.Purchases.Include(p => p.Items).ToQueryString();
Check(sql.Contains("purchase_items"), "Aggregate query could not be translated.");
db.Add(product);
db.Add(purchase);
Check(db.ChangeTracker.Entries().Count(e => e.State == EntityState.Added) == 5, "Aggregate children were not tracked.");
db.ChangeTracker.Clear();
var movement = new InventoryMovement(product.Id, lot.Id, userId, MovementType.Sale, -2, "Sale", sale.Id);
db.Attach(movement);
db.Entry(movement).State = EntityState.Modified;
Reject(() => db.SaveChanges());
db.Entry(movement).State = EntityState.Deleted;
Reject(() => db.SaveChanges());
Console.WriteLine($"OK: {checks} checks passed without database access.");
