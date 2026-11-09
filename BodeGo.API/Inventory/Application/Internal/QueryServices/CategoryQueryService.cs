using BodeGo.API.Inventory.Application.Model.Results;
using BodeGo.API.Inventory.Application.Services;
using BodeGo.API.Inventory.Domain;
using BodeGo.API.Inventory.Domain.Model.Queries;
using BodeGo.API.Inventory.Domain.Repositories;
namespace BodeGo.API.Inventory.Application.Internal.QueryServices;
public class CategoryQueryService(ICategoryRepository categories) : ICategoryQueryService
{
    public async Task<IReadOnlyList<CategoryResult>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken = default) =>
        (await categories.ListAsync(cancellationToken)).Select(CategoryResult.From).ToArray();
    public async Task<CategoryResult?> Handle(GetCategoryByIdQuery query, CancellationToken cancellationToken = default)
    {
        InventoryValidation.Id(query.Id);
        var category = await categories.FindByIdAsync(query.Id, cancellationToken);
        return category is null ? null : CategoryResult.From(category);
    }
}
