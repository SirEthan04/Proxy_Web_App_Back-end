using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Interfaces.REST.Resources;
namespace BodeGo.API.IAM.Interfaces.REST.Transform;
public static class UserResourceAssembler
{
    public static UserResource From(UserResult user) =>
        new(user.Id, user.Name, user.Email, user.RoleId, user.Active, user.CreatedAt);
}
