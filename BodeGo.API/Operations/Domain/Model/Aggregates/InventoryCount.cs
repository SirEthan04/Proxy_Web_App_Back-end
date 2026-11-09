using BodeGo.API.Operations.Domain.Model.Entities;
using BodeGo.API.Operations.Domain.Model.ValueObjects;
using BodeGo.API.Shared.Domain;
namespace BodeGo.API.Operations.Domain.Model.Aggregates;
public class InventoryCount
{
    private readonly List<InventoryCountItem> _items = [];
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CountedAt { get; private set; }
    public InventoryCountStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public IReadOnlyCollection<InventoryCountItem> Items => _items.AsReadOnly();
    private InventoryCount() { }
    public InventoryCount(Guid userId, string? notes = null)
    {
        Id = Guid.NewGuid();
        UserId = Validation.Id(userId, nameof(userId));
        Notes = notes is null ? null : Validation.Text(notes, 500, nameof(notes));
        CountedAt = DateTime.UtcNow;
        Status = InventoryCountStatus.Draft;
    }
    public void AddItem(Guid productId, Guid? lotId, int expected, int counted)
    {
        RequireDraft();
        if (_items.Any(i => i.ProductId == productId && i.LotId == lotId))
            throw new InvalidOperationException("This product and lot have already been counted.");
        if (_items.Any(i => i.ProductId == productId && (i.LotId == null || lotId == null)))
            throw new InvalidOperationException("Do not mix whole-product counts and lot counts.");
        _items.Add(new InventoryCountItem(Id, productId, lotId, expected, counted));
    }
    public void Confirm()
    {
        RequireDraft();
        if (_items.Count == 0) throw new InvalidOperationException("At least one item is required.");
        Status = InventoryCountStatus.Confirmed;
    }
    public void Cancel() { RequireDraft(); Status = InventoryCountStatus.Cancelled; }
    private void RequireDraft()
    {
        if (Status != InventoryCountStatus.Draft)
            throw new InvalidOperationException("Only draft counts can be changed.");
    }
}
