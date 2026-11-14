using BodeGo.API.Commercial.Domain.Model.ValueObjects;
using BodeGo.API.Shared.Domain;
namespace BodeGo.API.Commercial.Domain.Model.Entities;
public class SaleItem
{
    public Guid Id { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? LotId { get; private set; }
    public Quantity Quantity { get; private set; } = null!;
    public Money UnitPrice { get; private set; } = null!;
    public Money Subtotal { get; private set; } = null!;
    private SaleItem() { }
    internal SaleItem(Guid parentId, Guid productId, Guid? lotId, Quantity quantity, Money price)
    {
        Id = Guid.NewGuid();
        SaleId = Validation.Id(parentId, nameof(parentId));
        ProductId = Validation.Id(productId, nameof(productId));
        LotId = Validation.OptionalId(lotId, nameof(lotId));
        Quantity = quantity ?? throw new ArgumentNullException(nameof(quantity));
        UnitPrice = price ?? throw new ArgumentNullException(nameof(price));
        Subtotal = new Money(quantity.Value * price.Value);
    }
}
