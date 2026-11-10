using BodeGo.API.Monitoring.Domain.Model.Aggregates;
using BodeGo.API.Monitoring.Domain.Repositories;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
namespace BodeGo.API.Monitoring.Infrastructure.Persistence.EFC.Repositories;
public class AlertRepository(BodeGoDbContext context) : IAlertRepository
{
    public async Task<IReadOnlyList<Alert>> ListAsync(CancellationToken cancellationToken = default) =>
        await context.Alerts.AsNoTracking().OrderByDescending(a => a.CreatedAt).ThenBy(a => a.Id).ToListAsync(cancellationToken);
    public Task<Alert?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Alerts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
}
