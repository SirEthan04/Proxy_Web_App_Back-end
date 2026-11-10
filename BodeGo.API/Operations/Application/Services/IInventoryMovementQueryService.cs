using BodeGo.API.Operations.Domain.Model.Queries;
using BodeGo.API.Operations.Domain.Model.Entities;
namespace BodeGo.API.Operations.Application.Services;
public interface IInventoryMovementQueryService
{
    Task<IReadOnlyList<InventoryMovement>> Handle(GetInventoryMovementsQuery query, CancellationToken ct = default);
}
