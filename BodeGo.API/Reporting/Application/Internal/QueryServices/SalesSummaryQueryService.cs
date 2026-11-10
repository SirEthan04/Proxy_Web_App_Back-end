using BodeGo.API.Reporting.Application.Model.Queries;
using BodeGo.API.Reporting.Application.Model.ReadModels;
using BodeGo.API.Reporting.Application.Services;
namespace BodeGo.API.Reporting.Application.Internal.QueryServices;
public class SalesSummaryQueryService(ISalesSummaryRepository repository) : ISalesSummaryQueryService
{
    public Task<SalesSummaryReadModel> Handle(GetSalesSummaryQuery query, CancellationToken cancellationToken = default) =>
        repository.GetAsync(new ReportDateRange(query.From, query.To), cancellationToken);
}
