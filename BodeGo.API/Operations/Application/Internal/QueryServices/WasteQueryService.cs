using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Queries;
using BodeGo.API.Operations.Domain.Repositories;
namespace BodeGo.API.Operations.Application.Internal.QueryServices;
public class WasteQueryService(IOperationsRepository repository) : IWasteQueryService
{
    public Task<IReadOnlyList<Waste>> Handle(GetWastesQuery query, CancellationToken ct = default) =>
        repository.ListWastesAsync(ct);
}
