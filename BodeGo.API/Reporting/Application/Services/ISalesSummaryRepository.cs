using BodeGo.API.Reporting.Application.Model.Queries;
using BodeGo.API.Reporting.Application.Model.ReadModels;
namespace BodeGo.API.Reporting.Application.Services;
public interface ISalesSummaryRepository
{
    Task<SalesSummaryReadModel> GetAsync(ReportDateRange range, CancellationToken cancellationToken = default);
}
