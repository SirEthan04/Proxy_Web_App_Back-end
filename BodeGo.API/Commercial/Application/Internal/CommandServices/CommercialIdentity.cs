using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.Commercial.Domain.Exceptions;
namespace BodeGo.API.Commercial.Application.Internal.CommandServices;

public static class CommercialIdentity
{
    public static void Creating(AuthenticatedUser actor, Guid creator)
    {
        Authorize(actor, creator);
        if (actor.Id != creator) Denied();
    }
    public static void Authorize(AuthenticatedUser actor, Guid creator)
    {
        if (actor.Id == Guid.Empty || actor.Role is not ("Administrador" or "Empleado")
            || (!actor.IsAdministrator && actor.Id != creator)) Denied();
    }
    private static void Denied() => throw new CommercialConfirmationException(
        "No tiene permiso para realizar esta operación.", ConfirmationFailure.Forbidden);
}
