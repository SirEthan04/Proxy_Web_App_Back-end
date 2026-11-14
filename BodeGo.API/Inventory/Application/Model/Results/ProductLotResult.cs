using BodeGo.API.Inventory.Domain.Model.Entities;
namespace BodeGo.API.Inventory.Application.Model.Results;
public sealed record ProductLotResult(Guid Id, Guid ProductId, string LotCode, DateOnly? ExpirationDate, int QuantityAvailable, DateTime ReceivedAt)
{
    public static ProductLotResult From(ProductLot lot) => new(lot.Id, lot.ProductId, lot.LotCode, lot.ExpirationDate, lot.QuantityAvailable, lot.ReceivedAt);
}
