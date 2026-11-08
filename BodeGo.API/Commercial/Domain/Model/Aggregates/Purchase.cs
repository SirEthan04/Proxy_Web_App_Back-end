using BodeGo.API.Commercial.Domain.Model.Entities;
using BodeGo.API.Commercial.Domain.Model.ValueObjects;
using BodeGo.API.Shared.Domain;
namespace BodeGo.API.Commercial.Domain.Model.Aggregates;
public class Purchase
{
    private readonly List<PurchaseItem> _items = [];
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime RegisteredAt { get; private set; }
    public CommercialStatus Status { get; private set; }
    public Money Total { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<PurchaseItem> Items => _items.AsReadOnly();
    private Purchase() { }
    public Purchase(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = Validation.Id(userId, nameof(userId));
        RegisteredAt = CreatedAt = DateTime.UtcNow;
        Status = CommercialStatus.Draft;
        Total = new Money(0);
    }
    public void AddItem(Guid productId, Guid? lotId, Quantity quantity, Money price)
    {
        RequireDraft();
        var item = new PurchaseItem(Id, productId, lotId, quantity, price);
        var total = new Money(_items.Sum(i => i.Subtotal.Value) + item.Subtotal.Value);
        _items.Add(item);
        Total = total;
    }
    public void Confirm()
    {
        RequireDraft();
        if (_items.Count == 0) throw new InvalidOperationException("At least one item is required.");
        Total = new Money(_items.Sum(i => i.Subtotal.Value));
        Status = CommercialStatus.Confirmed;
    }
    public void Cancel()
    {
        RequireDraft();
        Status = CommercialStatus.Cancelled;
    }
    private void RequireDraft()
    {
        if (Status != CommercialStatus.Draft)
            throw new InvalidOperationException("Only draft operations can be changed.");
    }
}
