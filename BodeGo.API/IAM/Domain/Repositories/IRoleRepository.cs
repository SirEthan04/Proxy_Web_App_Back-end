using BodeGo.API.IAM.Domain.Model.Entities;
namespace BodeGo.API.IAM.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> FindByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default);
}
