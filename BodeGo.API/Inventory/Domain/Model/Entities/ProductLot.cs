using BodeGo.API.Shared.Domain;
namespace BodeGo.API.Inventory.Domain.Model.Entities;
public class ProductLot
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string LotCode { get; private set; } = null!;
    public DateOnly? ExpirationDate { get; private set; }
    public int QuantityAvailable { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    private ProductLot() { }
    internal ProductLot(Guid productId, string lotCode, DateOnly? expirationDate, int quantity)
    {
        Id = Guid.NewGuid();
        ProductId = Validation.Id(productId, nameof(productId));
        LotCode = Validation.Text(lotCode, 80, nameof(lotCode));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        QuantityAvailable = quantity;
        ExpirationDate = expirationDate;
        ReceivedAt = DateTime.UtcNow;
    }
    internal void Remove(int quantity)
    {
        if (quantity <= 0 || quantity > QuantityAvailable) throw new ArgumentOutOfRangeException(nameof(quantity));
        QuantityAvailable -= quantity;
    }
    internal void Receive(int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        QuantityAvailable = checked(QuantityAvailable + quantity);
    }
}
