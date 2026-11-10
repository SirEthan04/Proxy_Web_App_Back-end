using BodeGo.API.Commercial.Application.Services;
using BodeGo.API.Commercial.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Entities;
using Microsoft.EntityFrameworkCore.Diagnostics;

internal sealed class InjectedConfirmationFailure : Exception;
internal sealed class FailAfterConfirmationSave : SaveChangesInterceptor
{
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default) => throw new InjectedConfirmationFailure();
}
internal sealed class GateAfterHeaderLock(ICommercialConfirmationSession inner, bool purchase, Func<Task> pause)
    : ICommercialConfirmationSession
{
    public Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct) => inner.ExecuteAsync(operation, ct);
    public async Task<Purchase?> LockPurchaseAsync(Guid id, CancellationToken ct)
    {
        var value = await inner.LockPurchaseAsync(id, ct);
        if (purchase) await pause();
        return value;
    }
    public async Task<Sale?> LockSaleAsync(Guid id, CancellationToken ct)
    {
        var value = await inner.LockSaleAsync(id, ct);
        if (!purchase) await pause();
        return value;
    }
    public Task<Product?> LockProductAsync(Guid id, CancellationToken ct) => inner.LockProductAsync(id, ct);
    public Task<bool> IsActiveUserAsync(Guid id, CancellationToken ct) => inner.IsActiveUserAsync(id, ct);
    public void AddMovements(IEnumerable<InventoryMovement> movements) => inner.AddMovements(movements);
}
