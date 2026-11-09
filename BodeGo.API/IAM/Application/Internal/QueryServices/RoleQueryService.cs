using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Domain.Model.Queries;
using BodeGo.API.IAM.Domain.Repositories;
namespace BodeGo.API.IAM.Application.Internal.QueryServices;
public class RoleQueryService(IRoleRepository roles) : IRoleQueryService
{
    public async Task<IReadOnlyList<RoleResult>> Handle(GetRolesQuery query, CancellationToken cancellationToken = default) =>
        (await roles.ListAsync(cancellationToken)).Select(role => new RoleResult(role.Id, role.Name)).ToArray();

    public async Task<RoleResult?> Handle(GetRoleByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Id <= 0) throw new ArgumentException("El identificador debe ser positivo.");
        var role = await roles.FindByIdAsync(query.Id, cancellationToken);
        return role is null ? null : new RoleResult(role.Id, role.Name);
    }
}
