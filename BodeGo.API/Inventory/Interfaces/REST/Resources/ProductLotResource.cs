namespace BodeGo.API.Inventory.Interfaces.REST.Resources;
public sealed record ProductLotResource(Guid Id, Guid ProductId, string LotCode, DateOnly? ExpirationDate, int QuantityAvailable, DateTime ReceivedAt);
