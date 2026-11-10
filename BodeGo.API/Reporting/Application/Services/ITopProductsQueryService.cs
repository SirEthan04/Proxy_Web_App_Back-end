using BodeGo.API.Reporting.Application.Model.Queries;
using BodeGo.API.Reporting.Application.Model.ReadModels;
namespace BodeGo.API.Reporting.Application.Services;
public interface ITopProductsQueryService
{
    Task<IReadOnlyList<TopProductReadModel>> Handle(GetTopProductsQuery query, CancellationToken cancellationToken = default);
}
