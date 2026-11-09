using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Domain.Model.Commands;
namespace BodeGo.API.IAM.Application.Services;

public interface IUserCommandService
{
    Task<UserResult> Handle(CreateUserCommand command, CancellationToken cancellationToken = default);
}
