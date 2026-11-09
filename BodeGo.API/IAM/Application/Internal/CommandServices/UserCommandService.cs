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
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("El nombre es obligatorio.");
        if (command.Name.Trim().Length > 120)
            throw new ArgumentException("El nombre no debe superar los 120 caracteres.");
        if (string.IsNullOrWhiteSpace(command.Email))
            throw new ArgumentException("El correo electrónico es obligatorio.");
        EmailAddress email;
        try { email = new EmailAddress(command.Email); }
        catch (ArgumentException) { throw new ArgumentException("El formato del correo electrónico no es válido."); }
        if (string.IsNullOrWhiteSpace(command.Password))
            throw new ArgumentException("La contraseña es obligatoria.");
        if (command.Password.Length < 12)
            throw new ArgumentException("La contraseña debe tener al menos 12 caracteres.");
        if (command.Password.Length > 128)
            throw new ArgumentException("La contraseña no debe superar los 128 caracteres.");
        if (command.RoleId <= 0 || await roles.FindByIdAsync(command.RoleId, cancellationToken) is null)
            throw new RoleNotFoundException();
        if (await users.ExistsByEmailAsync(email, cancellationToken))
            throw new DuplicateEmailException();

        var user = new User(command.RoleId, command.Name, email, passwords.Hash(command.Password));
        await users.AddAsync(user, cancellationToken);
        return UserResult.From(user);
    }
}
