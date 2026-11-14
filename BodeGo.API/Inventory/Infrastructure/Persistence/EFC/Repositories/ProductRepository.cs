using BodeGo.API.Inventory.Domain.Exceptions;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.Entities;
using BodeGo.API.Inventory.Domain.Repositories;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
namespace BodeGo.API.Inventory.Infrastructure.Persistence.EFC.Repositories;
public class ProductRepository(BodeGoDbContext context) : IProductRepository
{
    public Task<Product?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default) =>
        await context.Products.AsNoTracking().OrderBy(p => p.Name).ThenBy(p => p.Id).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Product>> ListLowStockAsync(CancellationToken cancellationToken = default) =>
        await LowStockQuery(context).ToListAsync(cancellationToken);
    // Value Objects use converters; compare the mapped columns directly in SQL.
    // Comparing Stock.Value in LINQ would require an unsupported member translation.
    public static IQueryable<Product> LowStockQuery(BodeGoDbContext context) =>
        context.Products.FromSqlRaw("SELECT * FROM products WHERE stock <= minimum_stock")
            .AsNoTracking().OrderBy(p => p.Name).ThenBy(p => p.Id);
    public async Task<IReadOnlyList<ProductLot>> ListLotsAsync(Guid productId, CancellationToken cancellationToken = default) =>
        await context.ProductLots.AsNoTracking().Where(l => l.ProductId == productId)
            .OrderBy(l => l.ReceivedAt).ThenBy(l => l.Id).ToListAsync(cancellationToken);
    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        context.Products.Add(product);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1452 })
        {
            context.Entry(product).State = EntityState.Detached;
            throw new CategoryNotFoundException();
        }
    }
}
