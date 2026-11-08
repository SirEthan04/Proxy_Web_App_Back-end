using BodeGo.API.Operations.Domain.Model.ValueObjects;
using BodeGo.API.Shared.Domain;
namespace BodeGo.API.Operations.Domain.Model.Entities;
public class InventoryMovement
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? LotId { get; private set; }
    public Guid UserId { get; private set; }
    public MovementType MovementType { get; private set; }
    public int QuantityDelta { get; private set; }
    public string SourceType { get; private set; } = null!;
    public string SourceId { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    private InventoryMovement() { }
    public InventoryMovement(Guid productId, Guid? lotId, Guid userId, MovementType type,
        int quantityDelta, string sourceType, Guid sourceId)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (quantityDelta == 0 || (type == MovementType.Purchase && quantityDelta < 0) ||
            ((type == MovementType.Sale || type == MovementType.Waste) && quantityDelta > 0))
            throw new ArgumentOutOfRangeException(nameof(quantityDelta));
        Id = Guid.NewGuid();
        ProductId = Validation.Id(productId, nameof(productId));
        LotId = Validation.OptionalId(lotId, nameof(lotId));
        UserId = Validation.Id(userId, nameof(userId));
        MovementType = type;
        QuantityDelta = quantityDelta;
        SourceType = Validation.Text(sourceType, 30, nameof(sourceType));
        SourceId = Validation.Id(sourceId, nameof(sourceId)).ToString();
        CreatedAt = DateTime.UtcNow;
    }
}
