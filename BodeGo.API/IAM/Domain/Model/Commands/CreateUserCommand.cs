namespace BodeGo.API.IAM.Domain.Model.Commands;

public sealed class CreateUserCommand(string name, string email, string password, int roleId)
{
    public string Name { get; } = name;
    public string Email { get; } = email;
    public string Password { get; } = password;
    public int RoleId { get; } = roleId;
}
