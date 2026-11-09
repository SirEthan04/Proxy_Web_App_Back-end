using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Domain.Model.Queries;
using BodeGo.API.IAM.Domain.Repositories;
namespace BodeGo.API.IAM.Application.Internal.QueryServices;
public class UserQueryService(IUserRepository users) : IUserQueryService
{
    public async Task<IReadOnlyList<UserResult>> Handle(GetUsersQuery query, CancellationToken cancellationToken = default) =>
        (await users.ListAsync(cancellationToken)).Select(UserResult.From).ToArray();

    public async Task<UserResult?> Handle(GetUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Id == Guid.Empty) throw new ArgumentException("El identificador no puede estar vacio.");
        var user = await users.FindByIdAsync(query.Id, cancellationToken);
        return user is null ? null : UserResult.From(user);
    }
}
