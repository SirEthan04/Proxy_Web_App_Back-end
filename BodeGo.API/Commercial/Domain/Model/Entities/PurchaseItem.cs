using BodeGo.API.Commercial.Domain.Model.ValueObjects;
using BodeGo.API.Shared.Domain;
namespace BodeGo.API.Commercial.Domain.Model.Entities;
public class PurchaseItem
{
    public Guid Id { get; private set; }
    public Guid PurchaseId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? LotId { get; private set; }
    public Quantity Quantity { get; private set; } = null!;
    public Money UnitCost { get; private set; } = null!;
    public Money Subtotal { get; private set; } = null!;
    private PurchaseItem() { }
    internal void AssignReceivedLot(Guid lotId)
    {
        Validation.Id(lotId, nameof(lotId));
        if (LotId.HasValue && LotId != lotId)
            throw new InvalidOperationException("No se puede reemplazar el lote del detalle.");
        LotId = lotId;
    }
    internal PurchaseItem(Guid parentId, Guid productId, Guid? lotId, Quantity quantity, Money price)
    {
        Id = Guid.NewGuid();
        PurchaseId = Validation.Id(parentId, nameof(parentId));
        ProductId = Validation.Id(productId, nameof(productId));
        LotId = Validation.OptionalId(lotId, nameof(lotId));
        Quantity = quantity ?? throw new ArgumentNullException(nameof(quantity));
        UnitCost = price ?? throw new ArgumentNullException(nameof(price));
        Subtotal = new Money(quantity.Value * price.Value);
    }
}
