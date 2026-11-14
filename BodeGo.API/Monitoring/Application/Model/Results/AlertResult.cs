using BodeGo.API.Monitoring.Domain.Model.Aggregates;
namespace BodeGo.API.Monitoring.Application.Model.Results;
public sealed record AlertResult(Guid Id, Guid ProductId, Guid? LotId, Guid? AttendedByUserId,
    string AlertType, string Status, string Message, DateTime CreatedAt, DateTime? AttendedAt)
{
    public static AlertResult From(Alert alert) => new(alert.Id, alert.ProductId, alert.LotId,
        alert.AttendedByUserId, alert.AlertType.ToString(), alert.Status.ToString(), alert.Message,
        DateTime.SpecifyKind(alert.CreatedAt, DateTimeKind.Utc),
        alert.AttendedAt.HasValue ? DateTime.SpecifyKind(alert.AttendedAt.Value, DateTimeKind.Utc) : null);
}
