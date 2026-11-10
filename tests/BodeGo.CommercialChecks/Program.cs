using BodeGo.API.Commercial.Application.Internal.CommandServices;
using BodeGo.API.Commercial.Domain.Exceptions;
using BodeGo.API.Commercial.Domain.Model.Aggregates;
using BodeGo.API.Commercial.Domain.Model.Commands;
using BodeGo.API.Commercial.Domain.Model.ValueObjects;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.ValueObjects;
using Money = BodeGo.API.Commercial.Domain.Model.ValueObjects.Money;

var checks = 0;
void Check(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
    checks++;
}
async Task Reject(Func<Task> action, ConfirmationFailure failure)
{
    try { await action(); }
    catch (CommercialConfirmationException exception) when (exception.Failure == failure) { checks++; return; }
    throw new InvalidOperationException("No se rechazó la operación con el error esperado.");
}
var session = new MemoryConfirmationSession();
var product = new Product(Guid.NewGuid(), new ProductName("Producto unitario"),
    new BodeGo.API.Inventory.Domain.Model.ValueObjects.Money(2.5m), new StockLevel(0));
session.Products.Add(product.Id, product);
var purchase = new Purchase(session.UserId);
purchase.AddItem(product.Id, null, new Quantity(20), new Money(1));
session.Purchases.Add(purchase.Id, purchase);
var purchases = new PurchaseConfirmationService(session);
var sales = new SaleConfirmationService(session);
await Reject(() => purchases.Handle(new ConfirmPurchaseCommand(Guid.Empty)), ConfirmationFailure.Invalid);
await Reject(() => purchases.Handle(new ConfirmPurchaseCommand(Guid.NewGuid())), ConfirmationFailure.NotFound);
var received = await purchases.Handle(new ConfirmPurchaseCommand(purchase.Id));
var lot = product.Lots.Single();
Check(received.Status == "Confirmed" && received.Items.Single().LotId == lot.Id, "Compra sin lote no asignó lote recibido.");
Check(product.Stock.Value == 20 && lot.QuantityAvailable == 20 && product.Version == 1, "La compra no ingresó 20 unidades.");
Check(session.Movements.Single().QuantityDelta == 20 && session.Movements[0].SourceId == purchase.Id.ToString(), "Entrada sin trazabilidad.");
await Reject(() => purchases.Handle(new ConfirmPurchaseCommand(purchase.Id)), ConfirmationFailure.Conflict);
Check(product.Stock.Value == 20 && session.Movements.Count == 1, "Compra repetida duplicó ingresos.");
var sale = new Sale(session.UserId);
sale.AddItem(product.Id, lot.Id, new Quantity(3), new Money(2.5m));
session.Sales.Add(sale.Id, sale);
await sales.Handle(new ConfirmSaleCommand(sale.Id));
Check(sale.Status == CommercialStatus.Confirmed && product.Stock.Value == 17 && lot.QuantityAvailable == 17, "Venta no dejó stock 17.");
Check(product.Version == 2 && session.Movements.Last().QuantityDelta == -3 && session.Movements.Last().SourceId == sale.Id.ToString(), "Salida o versión incorrecta.");
await Reject(() => sales.Handle(new ConfirmSaleCommand(sale.Id)), ConfirmationFailure.Conflict);
var excessive = new Sale(session.UserId);
excessive.AddItem(product.Id, lot.Id, new Quantity(18), new Money(1));
session.Sales.Add(excessive.Id, excessive);
await Reject(() => sales.Handle(new ConfirmSaleCommand(excessive.Id)), ConfirmationFailure.Conflict);
Check(excessive.Status == CommercialStatus.Draft && product.Stock.Value == 17, "Stock insuficiente cambió datos.");
var replenishment = new Purchase(session.UserId);
replenishment.AddItem(product.Id, lot.Id, new Quantity(2), new Money(1));
replenishment.AddItem(product.Id, lot.Id, new Quantity(3), new Money(1));
session.Purchases.Add(replenishment.Id, replenishment);
await purchases.Handle(new ConfirmPurchaseCommand(replenishment.Id));
Check(product.Stock.Value == 22 && lot.QuantityAvailable == 22 && product.Lots.Count == 1, "Reposición duplicada no sumó al lote existente.");
var wrongLot = new Sale(session.UserId);
wrongLot.AddItem(product.Id, Guid.NewGuid(), new Quantity(1), new Money(1));
session.Sales.Add(wrongLot.Id, wrongLot);
await Reject(() => sales.Handle(new ConfirmSaleCommand(wrongLot.Id)), ConfirmationFailure.Invalid);
var cancelled = new Purchase(session.UserId);
cancelled.AddItem(product.Id, null, new Quantity(1), new Money(1));
cancelled.Cancel(); session.Purchases.Add(cancelled.Id, cancelled);
await Reject(() => purchases.Handle(new ConfirmPurchaseCommand(cancelled.Id)), ConfirmationFailure.Conflict);
var cancelledSale = new Sale(session.UserId);
cancelledSale.AddItem(product.Id, lot.Id, new Quantity(1), new Money(1));
cancelledSale.Cancel(); session.Sales.Add(cancelledSale.Id, cancelledSale);
await Reject(() => sales.Handle(new ConfirmSaleCommand(cancelledSale.Id)), ConfirmationFailure.Conflict);
var emptyPurchase = new Purchase(session.UserId);
session.Purchases.Add(emptyPurchase.Id, emptyPurchase);
await Reject(() => purchases.Handle(new ConfirmPurchaseCommand(emptyPurchase.Id)), ConfirmationFailure.Invalid);
var fifoLot = product.ReceiveLot("FIFO-2", null, 10);
var mixed = new Sale(session.UserId);
mixed.AddItem(product.Id, null, new Quantity(10), new Money(1));
mixed.AddItem(product.Id, lot.Id, new Quantity(20), new Money(1));
session.Sales.Add(mixed.Id, mixed);
await sales.Handle(new ConfirmSaleCommand(mixed.Id));
Check(product.Stock.Value == 2 && lot.QuantityAvailable == 0 && fifoLot.QuantityAvailable == 2, "FIFO consumió unidades reservadas explícitamente.");
Check(session.Movements.Where(m => m.SourceId == mixed.Id.ToString()).Sum(m => m.QuantityDelta) == -30, "Suma de movimientos de venta incorrecta.");
var overflow = new Product(Guid.NewGuid(), new ProductName("Overflow"), new BodeGo.API.Inventory.Domain.Model.ValueObjects.Money(0), new StockLevel(0));
var fullLot = overflow.ReceiveLot("MAX", null, int.MaxValue);
try { overflow.ReceiveStock(fullLot.Id, 1); throw new InvalidOperationException("Overflow no detectado."); }
catch (OverflowException) { Check(overflow.Stock.Value == int.MaxValue && fullLot.QuantityAvailable == int.MaxValue && overflow.Version == 1, "Overflow mutó el agregado."); }
Console.WriteLine($"OK: {checks} comprobaciones unitarias de Commercial. Los dobles no prueban transacciones MySQL.");
if (args is ["--integration", var url]) await CommercialIntegrationChecks.Run(url);
else if (args.Length != 0) throw new ArgumentException("Uso: --integration <URL local>");
