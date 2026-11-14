using BodeGo.API.Monitoring.Domain.Model.Queries;
using BodeGo.API.Monitoring.Domain.Repositories;
using BodeGo.API.Monitoring.Application.Model.Results;
using BodeGo.API.Monitoring.Application.Services;
namespace BodeGo.API.Monitoring.Application.Internal.QueryServices;
public class AlertQueryService(IAlertRepository alerts) : IAlertQueryService
{
    public async Task<IReadOnlyList<AlertResult>> Handle(GetAlertsQuery query, CancellationToken cancellationToken = default) =>
        (await alerts.ListAsync(cancellationToken)).Select(AlertResult.From).ToArray();
    public async Task<AlertResult?> Handle(GetAlertByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Id == Guid.Empty) throw new ArgumentException("El identificador de la alerta no puede estar vacío.");
        var alert = await alerts.FindByIdAsync(query.Id, cancellationToken);
        return alert is null ? null : AlertResult.From(alert);
    }
}
