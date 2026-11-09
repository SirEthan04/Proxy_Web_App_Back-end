using BodeGo.API.Commercial.Domain.Model.Aggregates;
namespace BodeGo.API.Commercial.Domain.Repositories;
public interface IPurchaseRepository
{
    Task AddAsync(Purchase purchase, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Purchase>> ListAsync(CancellationToken cancellationToken = default);
    Task<Purchase?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
