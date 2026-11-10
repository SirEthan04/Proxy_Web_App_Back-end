namespace BodeGo.API.Monitoring.Interfaces.REST.Resources;
public sealed record AlertResource(Guid Id, Guid ProductId, Guid? LotId, Guid? AttendedByUserId,
    string AlertType, string Status, string Message, DateTime CreatedAt, DateTime? AttendedAt);
