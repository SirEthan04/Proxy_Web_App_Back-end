using BodeGo.API.IAM.Application.Model.Results;
namespace BodeGo.API.IAM.Application.Services;
// Implementación HTTP en Infrastructure; Application no depende de ClaimsPrincipal.
public interface ICurrentUser
{
    AuthenticatedUser User { get; }
}
