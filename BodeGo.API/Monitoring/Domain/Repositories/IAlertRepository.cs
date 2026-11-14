using BodeGo.API.Monitoring.Domain.Model.Aggregates;
namespace BodeGo.API.Monitoring.Domain.Repositories;
public interface IAlertRepository
{
    Task<IReadOnlyList<Alert>> ListAsync(CancellationToken cancellationToken = default);
    Task<Alert?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
