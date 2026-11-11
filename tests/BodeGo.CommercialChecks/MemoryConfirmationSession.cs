using BodeGo.API.Commercial.Application.Services;
using BodeGo.API.Commercial.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Entities;

internal sealed class MemoryConfirmationSession : ICommercialConfirmationSession, BodeGo.API.IAM.Application.Services.ICurrentUser
{
    public Guid UserId { get; } = Guid.NewGuid();
    public BodeGo.API.IAM.Application.Model.Results.AuthenticatedUser User =>
        new(UserId, "Prueba", "commercial@example.test", 2, "Empleado");
    public Dictionary<Guid, Product> Products { get; } = [];
    public Dictionary<Guid, Purchase> Purchases { get; } = [];
    public Dictionary<Guid, Sale> Sales { get; } = [];
    public List<InventoryMovement> Movements { get; } = [];
    public Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct) => operation();
    public Task<Purchase?> LockPurchaseAsync(Guid id, CancellationToken ct) => Task.FromResult(Purchases.GetValueOrDefault(id));
    public Task<Sale?> LockSaleAsync(Guid id, CancellationToken ct) => Task.FromResult(Sales.GetValueOrDefault(id));
    public Task<Product?> LockProductAsync(Guid id, CancellationToken ct) => Task.FromResult(Products.GetValueOrDefault(id));
    public Task<bool> IsActiveUserAsync(Guid id, CancellationToken ct) => Task.FromResult(id == UserId);
    public void AddMovements(IEnumerable<InventoryMovement> movements) => Movements.AddRange(movements);
}
