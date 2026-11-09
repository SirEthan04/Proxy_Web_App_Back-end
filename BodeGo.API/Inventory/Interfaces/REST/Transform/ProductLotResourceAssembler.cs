using BodeGo.API.Inventory.Application.Model.Results;
using BodeGo.API.Inventory.Interfaces.REST.Resources;
namespace BodeGo.API.Inventory.Interfaces.REST.Transform;
public static class ProductLotResourceAssembler
{
    public static ProductLotResource From(ProductLotResult lot) => new(lot.Id, lot.ProductId, lot.LotCode, lot.ExpirationDate, lot.QuantityAvailable, lot.ReceivedAt);
}
