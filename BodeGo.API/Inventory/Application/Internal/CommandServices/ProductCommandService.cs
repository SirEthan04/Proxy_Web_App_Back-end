using BodeGo.API.Inventory.Application.Model.Results;
using BodeGo.API.Inventory.Application.Services;
using BodeGo.API.Inventory.Domain.Exceptions;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.Commands;
using BodeGo.API.Inventory.Domain.Model.ValueObjects;
using BodeGo.API.Inventory.Domain.Repositories;
namespace BodeGo.API.Inventory.Application.Internal.CommandServices;
public class ProductCommandService(IProductRepository products, ICategoryRepository categories) : IProductCommandService
{
    public async Task<ProductResult> Handle(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = new Product(command.CategoryId, new ProductName(command.Name),
            new Money(command.SalePrice), new StockLevel(command.MinimumStock));
        if (await categories.FindByIdAsync(command.CategoryId, cancellationToken) is null) throw new CategoryNotFoundException();
        await products.AddAsync(product, cancellationToken);
        return ProductResult.From(product);
    }
}
