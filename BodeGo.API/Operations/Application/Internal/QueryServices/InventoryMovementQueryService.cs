using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain.Model.Queries;
using BodeGo.API.Operations.Domain.Model.Entities;
using BodeGo.API.Operations.Domain.Repositories;
namespace BodeGo.API.Operations.Application.Internal.QueryServices;
public class InventoryMovementQueryService(IOperationsRepository repository) : IInventoryMovementQueryService
{
    public Task<IReadOnlyList<InventoryMovement>> Handle(GetInventoryMovementsQuery query, CancellationToken ct = default) =>
        repository.ListMovementsAsync(ct);
}
