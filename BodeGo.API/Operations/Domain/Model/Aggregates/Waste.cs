using BodeGo.API.Shared.Domain;
namespace BodeGo.API.Operations.Domain.Model.Aggregates;
public class Waste
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? LotId { get; private set; }
    public Guid UserId { get; private set; }
    public int Quantity { get; private set; }
    public string Reason { get; private set; } = null!;
    public DateTime RegisteredAt { get; private set; }
    private Waste() { }
    public Waste(Guid productId, Guid? lotId, Guid userId, int quantity, string reason)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        Id = Guid.NewGuid();
        ProductId = Validation.Id(productId, nameof(productId));
        LotId = Validation.OptionalId(lotId, nameof(lotId));
        UserId = Validation.Id(userId, nameof(userId));
        Quantity = quantity;
        Reason = Validation.Text(reason, 255, nameof(reason));
        RegisteredAt = DateTime.UtcNow;
    }
}
