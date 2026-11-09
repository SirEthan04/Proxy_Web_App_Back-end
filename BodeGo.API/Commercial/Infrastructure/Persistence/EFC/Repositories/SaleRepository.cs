using BodeGo.API.Commercial.Domain.Model.Aggregates;
using BodeGo.API.Commercial.Domain.Repositories;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
namespace BodeGo.API.Commercial.Infrastructure.Persistence.EFC.Repositories;
public class SaleRepository(BodeGoDbContext context) : ISaleRepository
{
    public async Task AddAsync(Sale sale, CancellationToken cancellationToken = default)
    { context.Sales.Add(sale); await context.SaveChangesAsync(cancellationToken); }
    public async Task<IReadOnlyList<Sale>> ListAsync(CancellationToken cancellationToken = default) =>
        await context.Sales.AsNoTracking().Include(x => x.Items).OrderByDescending(x => x.RegisteredAt).ToArrayAsync(cancellationToken);
    public Task<Sale?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Sales.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}
