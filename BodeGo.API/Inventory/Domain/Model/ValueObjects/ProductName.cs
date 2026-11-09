using BodeGo.API.Shared.Domain;
namespace BodeGo.API.Inventory.Domain.Model.ValueObjects;
public sealed record ProductName
{
    public string Value { get; }
    public ProductName(string value) => Value = Validation.Text(value, 150, nameof(value));
}
