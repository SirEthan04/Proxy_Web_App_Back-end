using BodeGo.API.Inventory.Domain.Model.Entities;
using BodeGo.API.Inventory.Domain.Model.ValueObjects;
using BodeGo.API.Shared.Domain;
namespace BodeGo.API.Inventory.Domain.Model.Aggregates;
public class Product
{
    private readonly List<ProductLot> _lots = [];
    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }
    public ProductName Name { get; private set; } = null!;
    public Money SalePrice { get; private set; } = null!;
    public StockLevel Stock { get; private set; } = null!;
    public StockLevel MinimumStock { get; private set; } = null!;
    public bool Active { get; private set; }
    public int Version { get; private set; }
    public IReadOnlyCollection<ProductLot> Lots => _lots.AsReadOnly();
    private Product() { }
    public Product(Guid categoryId, ProductName name, Money salePrice, StockLevel minimumStock)
    {
        Id = Guid.NewGuid();
        CategoryId = InventoryValidation.Id(categoryId);
        Name = name ?? throw new ArgumentNullException(nameof(name));
        SalePrice = salePrice ?? throw new ArgumentNullException(nameof(salePrice));
        MinimumStock = minimumStock ?? throw new ArgumentNullException(nameof(minimumStock));
        Stock = new StockLevel(0);
        Active = true;
    }
    public ProductLot ReceiveLot(string code, DateOnly? expirationDate, int quantity)
    {
        if (_lots.Any(l => l.LotCode.Equals(code?.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Lot code already exists for this product.");
        var lot = new ProductLot(Id, code!, expirationDate, quantity);
        var stock = new StockLevel(checked(Stock.Value + quantity));
        var version = checked(Version + 1);
        _lots.Add(lot);
        Stock = stock;
        Version = version;
        return lot;
    }
    public void ReceiveStock(Guid lotId, int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        var lot = _lots.SingleOrDefault(l => l.Id == lotId)
            ?? throw new ArgumentException("El lote no pertenece al producto.", nameof(lotId));
        var stock = new StockLevel(checked(Stock.Value + quantity));
        var version = checked(Version + 1);
        lot.Receive(quantity);
        Stock = stock;
        Version = version;
    }
    public void RemoveStock(Guid lotId, int quantity)
    {
        var lot = _lots.SingleOrDefault(l => l.Id == lotId)
            ?? throw new ArgumentException("Lot does not belong to this product.", nameof(lotId));
        if (quantity <= 0 || quantity > Stock.Value || quantity > lot.QuantityAvailable)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        var version = checked(Version + 1);
        lot.Remove(quantity);
        Stock = new StockLevel(Stock.Value - quantity);
        Version = version;
    }
}
