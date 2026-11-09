using BodeGo.API.Inventory.Domain.Model.Entities;
namespace BodeGo.API.Inventory.Application.Model.Results;
public sealed record CategoryResult(Guid Id, string Name, string? Description, bool Active)
{
    public static CategoryResult From(Category category) => new(category.Id, category.Name, category.Description, category.Active);
}
