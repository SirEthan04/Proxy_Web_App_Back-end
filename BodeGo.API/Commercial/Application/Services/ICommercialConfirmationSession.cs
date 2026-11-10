using BodeGo.API.Commercial.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Entities;
namespace BodeGo.API.Commercial.Application.Services;
public interface ICommercialConfirmationSession
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct);
    Task<Purchase?> LockPurchaseAsync(Guid id, CancellationToken ct);
    Task<Sale?> LockSaleAsync(Guid id, CancellationToken ct);
    Task<Product?> LockProductAsync(Guid id, CancellationToken ct);
    Task<bool> IsActiveUserAsync(Guid id, CancellationToken ct);
    void AddMovements(IEnumerable<InventoryMovement> movements);
}
