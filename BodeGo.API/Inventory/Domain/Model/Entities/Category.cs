using BodeGo.API.Shared.Domain;
namespace BodeGo.API.Inventory.Domain.Model.Entities;
public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool Active { get; private set; }
    private Category() { }
    public Category(string name, string? description = null)
    {
        Id = Guid.NewGuid();
        Name = InventoryValidation.Text(name, 100, "nombre de la categoría");
        Description = description is null ? null : InventoryValidation.Text(description, 255, "texto de la descripción");
        Active = true;
    }
    public void Deactivate() => Active = false;
}
