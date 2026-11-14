using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.Entities;
namespace BodeGo.API.Inventory.Domain.Repositories;
public interface IProductRepository
{
    Task<Product?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> ListLowStockAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductLot>> ListLotsAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
}
