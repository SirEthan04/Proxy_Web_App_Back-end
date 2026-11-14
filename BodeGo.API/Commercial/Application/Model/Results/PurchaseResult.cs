using BodeGo.API.Commercial.Domain.Model.Aggregates;
namespace BodeGo.API.Commercial.Application.Model.Results;
public sealed record PurchaseResult(Guid Id, Guid UserId, DateTime RegisteredAt, string Status, decimal Total, IReadOnlyList<CommercialItemResult> Items)
{
    public static PurchaseResult From(Purchase x) => new(x.Id, x.UserId, x.RegisteredAt, x.Status.ToString(), x.Total.Value,
        x.Items.Select(i => new CommercialItemResult(i.Id, i.ProductId, i.LotId, i.Quantity.Value, i.UnitCost.Value, i.Subtotal.Value)).ToArray());
}
