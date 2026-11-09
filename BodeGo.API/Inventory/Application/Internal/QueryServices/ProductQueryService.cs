using BodeGo.API.Inventory.Application.Model.Results;
using BodeGo.API.Inventory.Application.Services;
using BodeGo.API.Inventory.Domain;
using BodeGo.API.Inventory.Domain.Model.Queries;
using BodeGo.API.Inventory.Domain.Repositories;
namespace BodeGo.API.Inventory.Application.Internal.QueryServices;
public class ProductQueryService(IProductRepository products) : IProductQueryService
{
    public async Task<IReadOnlyList<ProductResult>> Handle(GetProductsQuery query, CancellationToken cancellationToken = default) =>
        (await products.ListAsync(cancellationToken)).Select(ProductResult.From).ToArray();
    public async Task<ProductResult?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken = default)
    {
        InventoryValidation.Id(query.Id);
        var product = await products.FindByIdAsync(query.Id, cancellationToken);
        return product is null ? null : ProductResult.From(product);
    }
    public async Task<IReadOnlyList<ProductResult>> Handle(GetLowStockProductsQuery query, CancellationToken cancellationToken = default) =>
        (await products.ListLowStockAsync(cancellationToken)).Select(ProductResult.From).ToArray();
    public async Task<IReadOnlyList<ProductLotResult>?> Handle(GetProductLotsQuery query, CancellationToken cancellationToken = default)
    {
        InventoryValidation.Id(query.ProductId);
        if (await products.FindByIdAsync(query.ProductId, cancellationToken) is null) return null;
        return (await products.ListLotsAsync(query.ProductId, cancellationToken)).Select(ProductLotResult.From).ToArray();
    }
}
