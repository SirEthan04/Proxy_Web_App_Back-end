using BodeGo.API.Commercial.Application.Model.Results;
using BodeGo.API.Commercial.Application.Services;
using BodeGo.API.Commercial.Domain.Exceptions;
using BodeGo.API.Commercial.Domain.Model.Commands;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Entities;
using BodeGo.API.Operations.Domain.Model.ValueObjects;
namespace BodeGo.API.Commercial.Application.Internal.CommandServices;

public class PurchaseConfirmationService(ICommercialConfirmationSession session) : IPurchaseConfirmationService
{
    public Task<PurchaseResult> Handle(ConfirmPurchaseCommand command, CancellationToken ct = default)
    {
        ConfirmationRules.Id(command.Id);
        return session.ExecuteAsync(async () =>
        {
            var purchase = await session.LockPurchaseAsync(command.Id, ct)
                ?? throw new CommercialConfirmationException("No se encontró la compra solicitada.", ConfirmationFailure.NotFound);
            ConfirmationRules.Draft(purchase.Status, purchase.Items.Count);
            if (!await session.IsActiveUserAsync(purchase.UserId, ct))
                throw new CommercialConfirmationException("El usuario no existe o está inactivo.");
            var products = new Dictionary<Guid, Product>();
            foreach (var id in purchase.Items.Select(i => i.ProductId).Distinct().OrderBy(id => id))
            {
                var product = await session.LockProductAsync(id, ct)
                    ?? throw new CommercialConfirmationException("No se encontró uno de los productos.");
                ConfirmationRules.Product(product);
                products.Add(id, product);
            }
            var movements = new List<InventoryMovement>();
            foreach (var item in purchase.Items.OrderBy(i => i.Id))
            {
                var product = products[item.ProductId];
                Guid lotId;
                if (item.LotId is Guid existingLot)
                {
                    if (product.Lots.All(l => l.Id != existingLot))
                        throw new CommercialConfirmationException("El lote no existe o no pertenece al producto.");
                    product.ReceiveStock(existingLot, item.Quantity.Value);
                    lotId = existingLot;
                }
                else
                {
                    var lot = product.ReceiveLot($"PUR-{purchase.Id:N}-{item.Id:N}", null, item.Quantity.Value);
                    lotId = lot.Id;
                    purchase.AssignReceivedLot(item.Id, lotId);
                }
                movements.Add(new InventoryMovement(product.Id, lotId, purchase.UserId,
                    MovementType.Purchase, item.Quantity.Value, "Purchase", purchase.Id));
            }
            purchase.Confirm();
            session.AddMovements(movements);
            return PurchaseResult.From(purchase);
        }, ct);
    }
}
