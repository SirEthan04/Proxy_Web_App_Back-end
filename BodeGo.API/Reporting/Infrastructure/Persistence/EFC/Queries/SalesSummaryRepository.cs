using BodeGo.API.Reporting.Application.Model.Queries;
using BodeGo.API.Reporting.Application.Model.ReadModels;
using BodeGo.API.Reporting.Application.Services;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
namespace BodeGo.API.Reporting.Infrastructure.Persistence.EFC.Queries;
public class SalesSummaryRepository(BodeGoDbContext context) : ISalesSummaryRepository
{
    public async Task<SalesSummaryReadModel> GetAsync(ReportDateRange range, CancellationToken cancellationToken = default)
    {
        var rows = await context.Database.SqlQueryRaw<SalesSummaryReadModel>("""
            SELECT COUNT(*) AS SalesCount, COALESCE(SUM(total), 0) AS TotalAmount
            FROM sales
            WHERE status = @status
              AND (@from IS NULL OR registered_at >= @from)
              AND (@to IS NULL OR registered_at < @to)
            """, ReportSqlParameters.Create(range)).ToListAsync(cancellationToken);
        return rows.Single();
    }
}
