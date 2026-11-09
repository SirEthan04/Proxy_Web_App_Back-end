using BodeGo.API.Inventory.Application.Model.Results;
using BodeGo.API.Inventory.Interfaces.REST.Resources;
namespace BodeGo.API.Inventory.Interfaces.REST.Transform;
public static class ProductResourceAssembler
{
    public static ProductResource From(ProductResult product) => new(product.Id, product.CategoryId, product.Name,
        product.SalePrice, product.Stock, product.MinimumStock, product.Active, product.Version);
}
