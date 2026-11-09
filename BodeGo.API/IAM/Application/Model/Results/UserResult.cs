using BodeGo.API.IAM.Domain.Model.Aggregates;
namespace BodeGo.API.IAM.Application.Model.Results;

public sealed record UserResult(Guid Id, string Name, string Email, int RoleId, bool Active, DateTime CreatedAt)
{
    public static UserResult From(User user) =>
        new(user.Id, user.Name, user.Email.Value, user.RoleId, user.Active, user.CreatedAt);
}
