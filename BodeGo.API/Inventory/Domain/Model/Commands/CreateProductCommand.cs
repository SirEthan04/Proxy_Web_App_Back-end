namespace BodeGo.API.Inventory.Domain.Model.Commands;
public sealed record CreateProductCommand(Guid CategoryId, string Name, decimal SalePrice, int MinimumStock);
