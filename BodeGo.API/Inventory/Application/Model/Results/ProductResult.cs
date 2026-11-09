using BodeGo.API.Inventory.Domain.Model.Aggregates;
namespace BodeGo.API.Inventory.Application.Model.Results;
public sealed record ProductResult(Guid Id, Guid CategoryId, string Name, decimal SalePrice, int Stock, int MinimumStock, bool Active, int Version)
{
    public static ProductResult From(Product product) => new(product.Id, product.CategoryId, product.Name.Value,
        product.SalePrice.Value, product.Stock.Value, product.MinimumStock.Value, product.Active, product.Version);
}
