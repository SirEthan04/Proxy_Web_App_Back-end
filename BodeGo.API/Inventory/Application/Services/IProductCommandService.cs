using BodeGo.API.Inventory.Application.Model.Results;
using BodeGo.API.Inventory.Domain.Model.Commands;
namespace BodeGo.API.Inventory.Application.Services;
public interface IProductCommandService
{
    Task<ProductResult> Handle(CreateProductCommand command, CancellationToken cancellationToken = default);
}
