using BodeGo.API.Commercial.Application.Model.Results;
using BodeGo.API.Commercial.Domain.Model.Commands;
using BodeGo.API.Commercial.Interfaces.REST.Resources;
namespace BodeGo.API.Commercial.Interfaces.REST.Transform;
public static class CommercialAssembler
{
    public static IReadOnlyList<CommercialItemCommand> ToCommands(IEnumerable<CommercialItemResource> items) => items.Select(x => new CommercialItemCommand(x.ProductId!.Value, x.LotId, x.Quantity!.Value, x.UnitAmount!.Value)).ToArray();
    public static CommercialResponse From(PurchaseResult x) => new(x.Id,x.UserId,x.RegisteredAt,x.Status,x.Total,x.Items.Select(Item).ToArray());
    public static CommercialResponse From(SaleResult x) => new(x.Id,x.UserId,x.RegisteredAt,x.Status,x.Total,x.Items.Select(Item).ToArray());
    private static CommercialItemResponse Item(CommercialItemResult x) => new(x.Id,x.ProductId,x.LotId,x.Quantity,x.UnitAmount,x.Subtotal);
}
