using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain;
using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Queries;
using BodeGo.API.Operations.Domain.Repositories;
namespace BodeGo.API.Operations.Application.Internal.QueryServices;
public class InventoryCountQueryService(IOperationsRepository repository) : IInventoryCountQueryService
{
    public Task<InventoryCount?> Handle(GetInventoryCountByIdQuery query, CancellationToken ct = default)
    {
        OperationsRules.Id(query.Id);
        return repository.FindCountAsync(query.Id, ct);
    }
}
