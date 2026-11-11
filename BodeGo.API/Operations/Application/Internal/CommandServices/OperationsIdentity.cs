using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.Operations.Domain.Exceptions;
namespace BodeGo.API.Operations.Application.Internal.CommandServices;

internal static class OperationsIdentity
{
    public static void RequireActor(AuthenticatedUser actor, Guid userId)
    {
        if (actor.Id == Guid.Empty || actor.Id != userId
            || actor.Role is not ("Administrador" or "Empleado"))
            throw new OperationsException("No tiene permiso para registrar operaciones a nombre de otro usuario.", forbidden: true);
    }
}
