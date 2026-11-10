using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Queries;
namespace BodeGo.API.Operations.Application.Services;
public interface IWasteQueryService
{
    Task<IReadOnlyList<Waste>> Handle(GetWastesQuery query, CancellationToken ct = default);
}
