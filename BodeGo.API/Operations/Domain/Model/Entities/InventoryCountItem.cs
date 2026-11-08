using BodeGo.API.Shared.Domain;
namespace BodeGo.API.Operations.Domain.Model.Entities;
public class InventoryCountItem
{
    public Guid Id { get; private set; }
    public Guid CountId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? LotId { get; private set; }
    public int ExpectedQuantity { get; private set; }
    public int CountedQuantity { get; private set; }
    public int Difference { get; private set; }
    private InventoryCountItem() { }
    internal InventoryCountItem(Guid countId, Guid productId, Guid? lotId, int expected, int counted)
    {
        Id = Guid.NewGuid();
        CountId = Validation.Id(countId, nameof(countId));
        ProductId = Validation.Id(productId, nameof(productId));
        LotId = Validation.OptionalId(lotId, nameof(lotId));
        ExpectedQuantity = Validation.NonNegative(expected, nameof(expected));
        CountedQuantity = Validation.NonNegative(counted, nameof(counted));
        Difference = counted - expected;
    }
}
