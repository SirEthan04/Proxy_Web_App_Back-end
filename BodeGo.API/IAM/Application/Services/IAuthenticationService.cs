using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Domain.Model.Commands;
using BodeGo.API.IAM.Domain.Model.Queries;
namespace BodeGo.API.IAM.Application.Services;
public interface IAuthenticationService
{
    Task<LoginResult?> LoginAsync(LoginCommand command, CancellationToken ct = default);
    Task<AuthenticatedUser?> GetCurrentUserAsync(GetCurrentUserQuery query, CancellationToken ct = default);
}
