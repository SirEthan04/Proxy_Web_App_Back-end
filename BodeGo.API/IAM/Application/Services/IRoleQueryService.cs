using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Domain.Model.Queries;
namespace BodeGo.API.IAM.Application.Services;
public interface IRoleQueryService
{
    Task<IReadOnlyList<RoleResult>> Handle(GetRolesQuery query, CancellationToken cancellationToken = default);
    Task<RoleResult?> Handle(GetRoleByIdQuery query, CancellationToken cancellationToken = default);
}
