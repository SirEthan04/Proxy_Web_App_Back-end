using BodeGo.API.Operations.Domain.Model.Entities;
using BodeGo.API.Operations.Interfaces.REST.Resources;
namespace BodeGo.API.Operations.Interfaces.REST.Transform;
public static class InventoryMovementResourceAssembler
{
    public static InventoryMovementResource From(InventoryMovement m) =>
        new(m.Id, m.ProductId, m.LotId, m.UserId, m.MovementType.ToString(),
            m.QuantityDelta, m.SourceType, m.SourceId, m.CreatedAt);
}
