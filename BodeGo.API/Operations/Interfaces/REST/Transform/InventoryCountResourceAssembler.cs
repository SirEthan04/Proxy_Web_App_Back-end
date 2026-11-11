using BodeGo.API.Operations.Domain.Exceptions;
using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Commands;
using BodeGo.API.Operations.Interfaces.REST.Resources;
namespace BodeGo.API.Operations.Interfaces.REST.Transform;
public static class InventoryCountResourceAssembler
{
    public static InventoryCountResource From(InventoryCount c) =>
        new(c.Id, c.UserId, c.CountedAt, c.Status.ToString(), c.Notes,
            c.Items.Select(i => new InventoryCountItemResource(i.Id, i.ProductId, i.LotId,
                i.ExpectedQuantity, i.CountedQuantity, i.Difference)).ToArray());
    public static CreateInventoryCountCommand ToCommand(CreateInventoryCountResource r, Guid userId)
    {
        if (r.Items.Any(i => i == null)) throw new OperationsException("Los detalles no pueden ser nulos.");
        return new(userId, r.Notes, r.Items.Select(i =>
            new CountItemCommand(i.ProductId!.Value, i.LotId, i.CountedQuantity!.Value)).ToArray());
    }
}
