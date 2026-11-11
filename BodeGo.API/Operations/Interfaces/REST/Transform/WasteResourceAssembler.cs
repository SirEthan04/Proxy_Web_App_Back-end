using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Commands;
using BodeGo.API.Operations.Interfaces.REST.Resources;
namespace BodeGo.API.Operations.Interfaces.REST.Transform;
public static class WasteResourceAssembler
{
    public static WasteResource From(Waste w) =>
        new(w.Id, w.ProductId, w.LotId, w.UserId, w.Quantity, w.Reason, w.RegisteredAt);
    public static CreateWasteCommand ToCommand(CreateWasteResource r, Guid userId) =>
        new(r.ProductId!.Value, r.LotId, userId, r.Quantity!.Value, r.Reason);
}
