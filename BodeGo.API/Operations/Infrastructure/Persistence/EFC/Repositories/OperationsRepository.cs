using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Entities;
using BodeGo.API.Operations.Domain.Repositories;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
namespace BodeGo.API.Operations.Infrastructure.Persistence.EFC.Repositories;
public class OperationsRepository(BodeGoDbContext context) : IOperationsRepository
{
    public async Task<IReadOnlyList<InventoryMovement>> ListMovementsAsync(CancellationToken ct) =>
        await context.InventoryMovements.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).ToArrayAsync(ct);
    public async Task<IReadOnlyList<Waste>> ListWastesAsync(CancellationToken ct) =>
        await context.Wastes.AsNoTracking().OrderByDescending(x => x.RegisteredAt).ThenBy(x => x.Id).ToArrayAsync(ct);
    public Task<InventoryCount?> FindCountAsync(Guid id, CancellationToken ct) =>
        context.InventoryCounts.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
    public void Add(Waste waste, IReadOnlyList<InventoryMovement> movements)
    {
        context.Wastes.Add(waste);
        context.InventoryMovements.AddRange(movements);
    }
    public void Add(InventoryCount count) => context.InventoryCounts.Add(count);
}
