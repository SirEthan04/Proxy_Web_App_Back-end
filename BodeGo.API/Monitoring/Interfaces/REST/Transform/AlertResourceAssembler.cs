using BodeGo.API.Monitoring.Application.Model.Results;
using BodeGo.API.Monitoring.Interfaces.REST.Resources;
namespace BodeGo.API.Monitoring.Interfaces.REST.Transform;
public static class AlertResourceAssembler
{
    public static AlertResource From(AlertResult alert) => new(alert.Id, alert.ProductId, alert.LotId,
        alert.AttendedByUserId, alert.AlertType, alert.Status, alert.Message, alert.CreatedAt, alert.AttendedAt);
}
