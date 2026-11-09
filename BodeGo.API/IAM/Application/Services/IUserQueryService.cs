using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Domain.Model.Queries;
namespace BodeGo.API.IAM.Application.Services;
public interface IUserQueryService
{
    Task<IReadOnlyList<UserResult>> Handle(GetUsersQuery query, CancellationToken cancellationToken = default);
    Task<UserResult?> Handle(GetUserByIdQuery query, CancellationToken cancellationToken = default);
}
