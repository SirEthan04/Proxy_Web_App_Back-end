using BodeGo.API.Inventory.Application.Model.Results;
using BodeGo.API.Inventory.Domain.Model.Queries;
namespace BodeGo.API.Inventory.Application.Services;
public interface ICategoryQueryService
{
    Task<IReadOnlyList<CategoryResult>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken = default);
    Task<CategoryResult?> Handle(GetCategoryByIdQuery query, CancellationToken cancellationToken = default);
}
