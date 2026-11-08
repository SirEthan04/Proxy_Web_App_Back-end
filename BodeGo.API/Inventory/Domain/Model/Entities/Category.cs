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
        Name = Validation.Text(name, 100, nameof(name));
        Description = description is null ? null : Validation.Text(description, 255, nameof(description));
        Active = true;
    }
    public void Deactivate() => Active = false;
}
