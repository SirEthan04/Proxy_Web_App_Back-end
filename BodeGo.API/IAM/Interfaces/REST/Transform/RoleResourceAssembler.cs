using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Interfaces.REST.Resources;
namespace BodeGo.API.IAM.Interfaces.REST.Transform;
public static class RoleResourceAssembler
{
    public static RoleResource From(RoleResult role) => new(role.Id, role.Name);
}
