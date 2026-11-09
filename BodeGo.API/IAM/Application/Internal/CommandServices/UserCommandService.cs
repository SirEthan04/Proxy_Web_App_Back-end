using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Domain.Exceptions;
using BodeGo.API.IAM.Domain.Model.Aggregates;
using BodeGo.API.IAM.Domain.Model.Commands;
using BodeGo.API.IAM.Domain.Model.ValueObjects;
using BodeGo.API.IAM.Domain.Repositories;
namespace BodeGo.API.IAM.Application.Internal.CommandServices;

public class UserCommandService(IUserRepository users, IRoleRepository roles, IPasswordHashService passwords)
    : IUserCommandService
{
    public async Task<UserResult> Handle(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > 120)
            throw new ArgumentException("El nombre es obligatorio y admite hasta 120 caracteres.");
        var email = new EmailAddress(command.Email);
        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length is < 12 or > 128)
            throw new ArgumentException("La contraseña debe tener entre 12 y 128 caracteres.");
        if (command.RoleId <= 0 || await roles.FindByIdAsync(command.RoleId, cancellationToken) is null)
            throw new RoleNotFoundException();
        if (await users.ExistsByEmailAsync(email, cancellationToken))
            throw new DuplicateEmailException();

        var user = new User(command.RoleId, command.Name, email, passwords.Hash(command.Password));
        await users.AddAsync(user, cancellationToken);
        return UserResult.From(user);
    }
}
