using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Domain.Model.Commands;
using BodeGo.API.IAM.Domain.Model.Queries;
using BodeGo.API.IAM.Domain.Model.ValueObjects;
using BodeGo.API.IAM.Domain.Repositories;
namespace BodeGo.API.IAM.Application.Internal.CommandServices;

public sealed class AuthenticationService(IUserRepository users, IRoleRepository roles,
    IPasswordHashService passwords, IAccessTokenService tokens) : IAuthenticationService
{
    public async Task<LoginResult?> LoginAsync(LoginCommand command, CancellationToken ct = default)
    {
        EmailAddress email;
        try { email = new EmailAddress(command.Email); }
        catch (ArgumentException) { passwords.Verify(null, command.Password); return null; }
        var user = await users.FindByEmailAsync(email, ct);
        var valid = passwords.Verify(user?.PasswordHash, command.Password);
        if (user is null || !valid || !user.Active) return null;
        var identity = await GetCurrentUserAsync(new(user.Id), ct);
        return identity is null ? null : tokens.Issue(identity);
    }

    public async Task<AuthenticatedUser?> GetCurrentUserAsync(GetCurrentUserQuery query, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(query.Id, ct);
        if (user is null || !user.Active) return null;
        var role = await roles.FindByIdAsync(user.RoleId, ct);
        if (role is null || role.Name is not ("Administrador" or "Empleado")) return null;
        return new(user.Id, user.Name, user.Email.Value, user.RoleId, role.Name);
    }
}
