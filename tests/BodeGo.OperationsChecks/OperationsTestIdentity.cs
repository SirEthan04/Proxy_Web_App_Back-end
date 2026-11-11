using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Domain.Model.Commands;
using BodeGo.API.IAM.Domain.Model.Queries;

// Doble explícito de la consulta de identidad; la suite AuthenticationChecks usa MySQL real.
internal sealed class OperationsTestIdentity(Guid id) : ICurrentUser, IAuthenticationService
{
    public AuthenticatedUser User => new(id, "Prueba Operations", "operations@example.test", 2, "Empleado");
    public Task<AuthenticatedUser?> GetCurrentUserAsync(GetCurrentUserQuery query, CancellationToken ct = default) =>
        Task.FromResult<AuthenticatedUser?>(query.Id == id ? User : null);
    public Task<LoginResult?> LoginAsync(LoginCommand command, CancellationToken ct = default) =>
        throw new NotSupportedException("El login real se prueba en AuthenticationChecks.");
}
