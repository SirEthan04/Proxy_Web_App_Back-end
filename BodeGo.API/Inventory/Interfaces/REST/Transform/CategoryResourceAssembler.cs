using BodeGo.API.Inventory.Application.Model.Results;
using BodeGo.API.Inventory.Interfaces.REST.Resources;
namespace BodeGo.API.Inventory.Interfaces.REST.Transform;
public static class CategoryResourceAssembler
{
    public static CategoryResource From(CategoryResult category) => new(category.Id, category.Name, category.Description, category.Active);
}
