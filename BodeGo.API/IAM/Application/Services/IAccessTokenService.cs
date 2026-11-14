using BodeGo.API.IAM.Application.Model.Results;
namespace BodeGo.API.IAM.Application.Services;
public interface IAccessTokenService
{
    LoginResult Issue(AuthenticatedUser user);
}
