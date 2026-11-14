using BodeGo.API.Commercial.Application.Model.Results;
using BodeGo.API.Commercial.Application.Services;
using BodeGo.API.Commercial.Domain.Exceptions;
using BodeGo.API.Commercial.Domain.Model.Commands;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Entities;
using BodeGo.API.Operations.Domain.Model.ValueObjects;
using BodeGo.API.IAM.Application.Services;
namespace BodeGo.API.Commercial.Application.Internal.CommandServices;

public class SaleConfirmationService(ICommercialConfirmationSession session, ICurrentUser currentUser) : ISaleConfirmationService
{
    public Task<SaleResult> Handle(ConfirmSaleCommand command, CancellationToken ct = default)
    {
        ConfirmationRules.Id(command.Id);
        return session.ExecuteAsync(async () =>
        {
            var sale = await session.LockSaleAsync(command.Id, ct)
                ?? throw new CommercialConfirmationException("No se encontró la venta solicitada.", ConfirmationFailure.NotFound);
            CommercialIdentity.Authorize(currentUser.User, sale.UserId);
            ConfirmationRules.Draft(sale.Status, sale.Items.Count);
            if (!await session.IsActiveUserAsync(currentUser.User.Id, ct))
                throw new CommercialConfirmationException("El usuario no existe o está inactivo.");
            var products = new Dictionary<Guid, Product>();
            foreach (var group in sale.Items.GroupBy(i => i.ProductId).OrderBy(g => g.Key))
            {
                var product = await session.LockProductAsync(group.Key, ct)
                    ?? throw new CommercialConfirmationException("No se encontró uno de los productos.");
                ConfirmationRules.Product(product);
                if (group.Sum(i => (long)i.Quantity.Value) > product.Stock.Value)
                    throw InsufficientStock();
                foreach (var lotGroup in group.Where(i => i.LotId.HasValue).GroupBy(i => i.LotId!.Value))
                {
                    var lot = product.Lots.SingleOrDefault(l => l.Id == lotGroup.Key)
                        ?? throw new CommercialConfirmationException("El lote no existe o no pertenece al producto.");
                    if (lotGroup.Sum(i => (long)i.Quantity.Value) > lot.QuantityAvailable)
                        throw InsufficientStock();
                }
                products.Add(product.Id, product);
            }
            var movements = new List<InventoryMovement>();
            // Reservar primero los lotes explícitos evita que el FIFO consuma sus unidades.
            foreach (var item in sale.Items.OrderBy(i => i.LotId.HasValue ? 0 : 1).ThenBy(i => i.Id))
            {
                var product = products[item.ProductId];
                var lots = product.Lots.Where(l => item.LotId == null || l.Id == item.LotId)
                    .OrderBy(l => l.ReceivedAt).ThenBy(l => l.Id).ToArray();
                var remaining = item.Quantity.Value;
                foreach (var lot in lots)
                {
                    var quantity = Math.Min(remaining, lot.QuantityAvailable);
                    if (quantity == 0) continue;
                    product.RemoveStock(lot.Id, quantity);
                    movements.Add(new InventoryMovement(product.Id, lot.Id, currentUser.User.Id,
                        MovementType.Sale, -quantity, "Sale", sale.Id));
                    remaining -= quantity;
                    if (remaining == 0) break;
                }
                if (remaining != 0) throw InsufficientStock();
            }
            sale.Confirm();
            session.AddMovements(movements);
            return SaleResult.From(sale);
        }, ct);
    }
    private static CommercialConfirmationException InsufficientStock() =>
        new("Stock insuficiente del producto o lote para confirmar la venta.", ConfirmationFailure.Conflict);
}
