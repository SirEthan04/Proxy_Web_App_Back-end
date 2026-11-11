using BodeGo.API.IAM.Domain.Model.Aggregates;
using BodeGo.API.IAM.Domain.Model.Entities;
using BodeGo.API.IAM.Domain.Model.ValueObjects;
using BodeGo.API.IAM.Domain.Repositories;
using BodeGo.API.Commercial.Application.Services;
using BodeGo.API.Commercial.Domain.Model.Aggregates;
using BodeGo.API.Commercial.Domain.Repositories;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Entities;
using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain.Repositories;
using BodeGo.API.Operations.Domain.Model.Aggregates;

internal sealed class TestRepositories : IUserRepository, IRoleRepository,
    IPurchaseRepository, ISaleRepository, ICommercialConfirmationSession, IOperationsSession, IOperationsRepository
{
    public List<User> Users { get; } = [];
    public List<Role> Roles { get; } = [new(1, "Administrador"), new(2, "Empleado")];
    public List<Purchase> Purchases { get; } = [];
    public List<Sale> Sales { get; } = [];
    public List<Product> Products { get; } = [];
    public List<InventoryMovement> Movements { get; } = [];
    public List<Waste> Wastes { get; } = [];
    public List<InventoryCount> Counts { get; } = [];
    public Task<Product?> LoadProductAsync(Guid id, CancellationToken ct) => Task.FromResult(Products.SingleOrDefault(p => p.Id == id));
    public void Add(Waste waste, IReadOnlyList<InventoryMovement> movements) { Wastes.Add(waste); Movements.AddRange(movements); }
    public void Add(InventoryCount count) => Counts.Add(count);
    public Task<IReadOnlyList<Waste>> ListWastesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Waste>>(Wastes.ToArray());
    public Task<IReadOnlyList<InventoryMovement>> ListMovementsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<InventoryMovement>>(Movements.ToArray());
    public Task<InventoryCount?> FindCountAsync(Guid id, CancellationToken ct) => Task.FromResult(Counts.SingleOrDefault(c => c.Id == id));
    public Task<User?> FindByEmailAsync(EmailAddress email, CancellationToken ct = default) => Task.FromResult(Users.SingleOrDefault(u => u.Email == email));
    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Users.SingleOrDefault(u => u.Id == id));
    public Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken ct = default) => Task.FromResult(Users.Any(u => u.Email == email));
    public Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<User>>(Users.ToArray());
    public Task AddAsync(User user, CancellationToken ct = default) { Users.Add(user); return Task.CompletedTask; }
    public Task<Role?> FindByIdAsync(int id, CancellationToken ct = default) => Task.FromResult(Roles.SingleOrDefault(r => r.Id == id));
    Task<IReadOnlyList<Role>> IRoleRepository.ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Role>>(Roles.ToArray());
    public Task AddAsync(Purchase purchase, CancellationToken ct = default) { Purchases.Add(purchase); return Task.CompletedTask; }
    public Task AddAsync(Sale sale, CancellationToken ct = default) { Sales.Add(sale); return Task.CompletedTask; }
    Task<IReadOnlyList<Purchase>> IPurchaseRepository.ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Purchase>>(Purchases.ToArray());
    Task<IReadOnlyList<Sale>> ISaleRepository.ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Sale>>(Sales.ToArray());
    Task<Purchase?> IPurchaseRepository.FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Purchases.SingleOrDefault(p => p.Id == id));
    Task<Sale?> ISaleRepository.FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Sales.SingleOrDefault(s => s.Id == id));
    public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct) => action();
    public Task<Purchase?> LockPurchaseAsync(Guid id, CancellationToken ct) => Task.FromResult(Purchases.SingleOrDefault(p => p.Id == id));
    public Task<Sale?> LockSaleAsync(Guid id, CancellationToken ct) => Task.FromResult(Sales.SingleOrDefault(s => s.Id == id));
    public Task<Product?> LockProductAsync(Guid id, CancellationToken ct) => Task.FromResult(Products.SingleOrDefault(p => p.Id == id));
    public Task<bool> IsActiveUserAsync(Guid id, CancellationToken ct) => Task.FromResult(Users.Any(u => u.Id == id && u.Active));
    public void AddMovements(IEnumerable<InventoryMovement> movements) => Movements.AddRange(movements);
}
