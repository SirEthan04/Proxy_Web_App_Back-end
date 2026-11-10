using BodeGo.API.Reporting.Application.Model.Queries;
using BodeGo.API.Reporting.Application.Model.ReadModels;
using BodeGo.API.Reporting.Application.Services;
namespace BodeGo.API.Reporting.Application.Internal.QueryServices;
public class TopProductsQueryService(ITopProductsRepository repository) : ITopProductsQueryService
{
    public Task<IReadOnlyList<TopProductReadModel>> Handle(GetTopProductsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Limit is < 1 or > 100) throw new ArgumentException("El límite debe estar entre 1 y 100.");
        return repository.GetAsync(new ReportDateRange(query.From, query.To), query.Limit, cancellationToken);
    }
}
