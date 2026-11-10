using BodeGo.API.Commercial.Domain.Exceptions;
using BodeGo.API.Commercial.Domain.Model.ValueObjects;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
namespace BodeGo.API.Commercial.Application.Internal.CommandServices;
internal static class ConfirmationRules
{
    public static void Id(Guid id)
    {
        if (id == Guid.Empty) throw new CommercialConfirmationException("El identificador no puede estar vacío.");
    }
    public static void Draft(CommercialStatus status, int items)
    {
        if (status != CommercialStatus.Draft)
            throw new CommercialConfirmationException("La operación ya fue confirmada o cancelada.", ConfirmationFailure.Conflict);
        if (items == 0) throw new CommercialConfirmationException("La operación debe incluir al menos un detalle.");
    }
    public static void Product(Product product)
    {
        if (!product.Active) throw new CommercialConfirmationException("Uno de los productos está inactivo.");
        if (product.Stock.Value != product.Lots.Sum(l => (long)l.QuantityAvailable))
            throw new CommercialConfirmationException("El stock no coincide con la suma de lotes. Revise Inventory.", ConfirmationFailure.Conflict);
    }
}
