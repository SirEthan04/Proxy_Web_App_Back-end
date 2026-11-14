using BodeGo.API.Reporting.Application.Model.Queries;
using BodeGo.API.Reporting.Application.Model.ReadModels;
namespace BodeGo.API.Reporting.Application.Services;
public interface ITopProductsRepository
{
    Task<IReadOnlyList<TopProductReadModel>> GetAsync(ReportDateRange range, int limit, CancellationToken cancellationToken = default);
}
