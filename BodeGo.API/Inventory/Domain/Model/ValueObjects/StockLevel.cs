namespace BodeGo.API.Inventory.Domain.Model.ValueObjects;
public sealed record StockLevel
{
    public int Value { get; }
    public StockLevel(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "El stock debe ser un número entero no negativo.");
        Value = value;
    }
}
