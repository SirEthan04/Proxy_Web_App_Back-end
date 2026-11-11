using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Application.Services;
internal sealed class CheckCurrentUser(Guid id) : ICurrentUser
{
    public AuthenticatedUser User => new(id, "Prueba aislada", "check@example.test", 2, "Empleado");
}
