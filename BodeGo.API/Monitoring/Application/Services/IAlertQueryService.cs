using BodeGo.API.Monitoring.Domain.Model.Queries;
using BodeGo.API.Monitoring.Application.Model.Results;
namespace BodeGo.API.Monitoring.Application.Services;
public interface IAlertQueryService
{
    Task<IReadOnlyList<AlertResult>> Handle(GetAlertsQuery query, CancellationToken cancellationToken = default);
    Task<AlertResult?> Handle(GetAlertByIdQuery query, CancellationToken cancellationToken = default);
}
