using BodeGo.API.Inventory.Application.Model.Results;
using BodeGo.API.Inventory.Domain.Model.Commands;
namespace BodeGo.API.Inventory.Application.Services;
public interface ICategoryCommandService
{
    Task<CategoryResult> Handle(CreateCategoryCommand command, CancellationToken cancellationToken = default);
}
