namespace BodeGo.API.Inventory.Interfaces.REST.Resources;
public sealed record ProductResource(Guid Id, Guid CategoryId, string Name, decimal SalePrice, int Stock, int MinimumStock, bool Active, int Version);
