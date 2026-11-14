using BodeGo.API.Inventory.Domain.Model.Entities;
namespace BodeGo.API.Inventory.Domain.Repositories;
public interface ICategoryRepository
{
    Task<Category?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
}
