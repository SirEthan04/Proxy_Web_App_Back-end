using BodeGo.API.Inventory.Application.Model.Results;
using BodeGo.API.Inventory.Application.Services;
using BodeGo.API.Inventory.Domain.Exceptions;
using BodeGo.API.Inventory.Domain.Model.Commands;
using BodeGo.API.Inventory.Domain.Model.Entities;
using BodeGo.API.Inventory.Domain.Repositories;
namespace BodeGo.API.Inventory.Application.Internal.CommandServices;
public class CategoryCommandService(ICategoryRepository categories) : ICategoryCommandService
{
    public async Task<CategoryResult> Handle(CreateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = new Category(command.Name, command.Description);
        if (await categories.ExistsByNameAsync(category.Name, cancellationToken)) throw new DuplicateCategoryException();
        await categories.AddAsync(category, cancellationToken);
        return CategoryResult.From(category);
    }
}
