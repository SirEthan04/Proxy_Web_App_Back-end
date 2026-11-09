using BodeGo.API.Monitoring.Domain.Model.ValueObjects;
using BodeGo.API.Shared.Domain;
namespace BodeGo.API.Monitoring.Domain.Model.Aggregates;
public class Alert
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? LotId { get; private set; }
    public Guid? AttendedByUserId { get; private set; }
    public AlertType AlertType { get; private set; }
    public AlertStatus Status { get; private set; }
    public string Message { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? AttendedAt { get; private set; }
    private Alert() { }
    public Alert(Guid productId, Guid? lotId, AlertType type, string message)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (type == AlertType.Expiration && lotId is null)
            throw new ArgumentException("Expiration alerts require a lot.", nameof(lotId));
        Id = Guid.NewGuid();
        ProductId = Validation.Id(productId, nameof(productId));
        LotId = Validation.OptionalId(lotId, nameof(lotId));
        AlertType = type;
        Status = AlertStatus.Pending;
        Message = Validation.Text(message, 255, nameof(message));
        CreatedAt = DateTime.UtcNow;
    }
    public void Attend(Guid userId)
    {
        if (Status != AlertStatus.Pending) throw new InvalidOperationException("Alert already attended.");
        AttendedByUserId = Validation.Id(userId, nameof(userId));
        AttendedAt = DateTime.UtcNow;
        Status = AlertStatus.Attended;
    }
}
