using BodeGo.API.IAM.Domain.Model.Aggregates;
using BodeGo.API.IAM.Domain.Model.ValueObjects;
namespace BodeGo.API.IAM.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
