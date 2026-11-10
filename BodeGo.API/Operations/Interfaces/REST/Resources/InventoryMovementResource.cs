namespace BodeGo.API.Operations.Interfaces.REST.Resources;
public record InventoryMovementResource(Guid Id, Guid ProductId, Guid? LotId, Guid UserId,
    string MovementType, int QuantityDelta, string SourceType, string SourceId, DateTime CreatedAt);
