using BodeGo.API.Reporting.Application.Model.Queries;
using BodeGo.API.Reporting.Application.Model.ReadModels;
using BodeGo.API.Reporting.Application.Services;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
namespace BodeGo.API.Reporting.Infrastructure.Persistence.EFC.Queries;
public class TopProductsRepository(BodeGoDbContext context) : ITopProductsRepository
{
    public async Task<IReadOnlyList<TopProductReadModel>> GetAsync(ReportDateRange range, int limit, CancellationToken cancellationToken = default)
    {
        var parameters = ReportSqlParameters.Create(range).Append(
            new MySqlParameter("@limit", MySqlDbType.Int32) { Value = limit }).ToArray();
        var rows = await context.Database.SqlQueryRaw<ProductRow>("""
            SELECT p.id AS ProductId, p.name AS ProductName,
                   CAST(SUM(i.quantity) AS SIGNED) AS QuantitySold
            FROM sale_items i
            JOIN sales s ON s.id = i.sale_id
            JOIN products p ON p.id = i.product_id
            WHERE s.status = @status
              AND (@from IS NULL OR s.registered_at >= @from)
              AND (@to IS NULL OR s.registered_at < @to)
            GROUP BY p.id, p.name
            ORDER BY QuantitySold DESC, p.id ASC
            LIMIT @limit
            """, parameters).ToListAsync(cancellationToken);
        return rows.Select(row => new TopProductReadModel(Guid.Parse(row.ProductId), row.ProductName, row.QuantitySold)).ToArray();
    }

    private sealed class ProductRow
    {
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";
        public long QuantitySold { get; set; }
    }
}
