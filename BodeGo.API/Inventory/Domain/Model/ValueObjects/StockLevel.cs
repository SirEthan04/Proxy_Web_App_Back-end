namespace BodeGo.API.Inventory.Domain.Model.ValueObjects;
public sealed record StockLevel
{
    public int Value { get; }
    public StockLevel(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        Value = value;
    }
}
