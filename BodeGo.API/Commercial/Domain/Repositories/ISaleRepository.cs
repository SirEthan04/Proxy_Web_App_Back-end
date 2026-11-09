using BodeGo.API.Commercial.Domain.Model.Aggregates;
namespace BodeGo.API.Commercial.Domain.Repositories;
public interface ISaleRepository
{
    Task AddAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sale>> ListAsync(CancellationToken cancellationToken = default);
    Task<Sale?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
