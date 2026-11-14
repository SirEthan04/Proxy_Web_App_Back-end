using BodeGo.API.Reporting.Application.Model.Queries;
using BodeGo.API.Reporting.Application.Model.ReadModels;
namespace BodeGo.API.Reporting.Application.Services;
public interface ISalesSummaryQueryService
{
    Task<SalesSummaryReadModel> Handle(GetSalesSummaryQuery query, CancellationToken cancellationToken = default);
}
