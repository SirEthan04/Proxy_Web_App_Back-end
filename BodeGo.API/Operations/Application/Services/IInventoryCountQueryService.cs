using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Queries;
namespace BodeGo.API.Operations.Application.Services;
public interface IInventoryCountQueryService
{
    Task<InventoryCount?> Handle(GetInventoryCountByIdQuery query, CancellationToken ct = default);
}
