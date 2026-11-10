using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Exceptions;
using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Entities;
using BodeGo.API.Operations.Domain.Model.ValueObjects;
namespace BodeGo.API.Operations.Domain.Services;
public static class WasteStockPolicy
{
    public static IReadOnlyList<InventoryMovement> Apply(Product product, Waste waste)
    {
        if (waste.ProductId != product.Id) throw new OperationsException("El producto de la merma no corresponde.");
        var lots = product.Lots.Where(l => waste.LotId == null || l.Id == waste.LotId)
            .OrderBy(l => l.ReceivedAt).ThenBy(l => l.Id).ToArray();
        if (waste.LotId.HasValue && lots.Length == 0)
            throw new OperationsException("El lote no existe o no corresponde al producto.");
        if (product.Stock.Value != product.Lots.Sum(l => (long)l.QuantityAvailable))
            throw new OperationsException("El stock y los lotes son inconsistentes; revise Inventory.", true);
        if (lots.Sum(l => (long)l.QuantityAvailable) < waste.Quantity || product.Stock.Value < waste.Quantity)
            throw new OperationsException("Stock insuficiente para registrar la merma.", true);
        var remaining = waste.Quantity;
        var movements = new List<InventoryMovement>();
        foreach (var lot in lots)
        {
            var quantity = Math.Min(remaining, lot.QuantityAvailable);
            if (quantity == 0) continue;
            product.RemoveStock(lot.Id, quantity);
            movements.Add(new InventoryMovement(product.Id, lot.Id, waste.UserId, MovementType.Waste,
                -quantity, "Waste", waste.Id));
            remaining -= quantity;
            if (remaining == 0) break;
        }
        return movements;
    }
}
