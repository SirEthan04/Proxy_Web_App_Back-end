using BodeGo.API.Inventory.Application.Model.Results;
using BodeGo.API.Inventory.Domain.Model.Queries;
namespace BodeGo.API.Inventory.Application.Services;
public interface IProductQueryService
{
    Task<IReadOnlyList<ProductResult>> Handle(GetProductsQuery query, CancellationToken cancellationToken = default);
    Task<ProductResult?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductResult>> Handle(GetLowStockProductsQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductLotResult>?> Handle(GetProductLotsQuery query, CancellationToken cancellationToken = default);
}
