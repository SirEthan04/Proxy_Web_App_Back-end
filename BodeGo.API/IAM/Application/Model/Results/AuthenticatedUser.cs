namespace BodeGo.API.IAM.Application.Model.Results;
public sealed record AuthenticatedUser(Guid Id, string Name, string Email, int RoleId, string Role)
{
    public bool IsAdministrator => Role == "Administrador";
}
