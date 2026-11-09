using BodeGo.API.Shared.Domain;
namespace BodeGo.API.Inventory.Domain.Model.ValueObjects;
public sealed record ProductName
{
    public string Value { get; }
    public ProductName(string value) => Value = InventoryValidation.Text(value, 150, "nombre del producto");
}
