using BodeGo.API.Commercial.Domain.Model.Aggregates;
using BodeGo.API.Commercial.Domain.Repositories;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
namespace BodeGo.API.Commercial.Infrastructure.Persistence.EFC.Repositories;
public class PurchaseRepository(BodeGoDbContext context) : IPurchaseRepository
{
    public async Task AddAsync(Purchase purchase, CancellationToken cancellationToken = default)
    { context.Purchases.Add(purchase); await context.SaveChangesAsync(cancellationToken); }
    public async Task<IReadOnlyList<Purchase>> ListAsync(CancellationToken cancellationToken = default) =>
        await context.Purchases.AsNoTracking().Include(x => x.Items).OrderByDescending(x => x.RegisteredAt).ToArrayAsync(cancellationToken);
    public Task<Purchase?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Purchases.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}
