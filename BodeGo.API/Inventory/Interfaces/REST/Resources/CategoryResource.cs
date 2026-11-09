namespace BodeGo.API.Inventory.Interfaces.REST.Resources;
public sealed record CategoryResource(Guid Id, string Name, string? Description, bool Active);
