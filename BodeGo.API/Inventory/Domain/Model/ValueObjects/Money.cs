namespace BodeGo.API.Inventory.Domain.Model.ValueObjects;

public sealed record Money
{
    public decimal Value { get; }
    public Money(decimal value)
    {
        if (value < 0 || value > 9999999999.99m || decimal.Round(value, 2) != value)
            throw new ArgumentOutOfRangeException(nameof(value), "El precio debe estar entre 0 y 9999999999.99 y tener como máximo dos decimales.");
        Value = value;
    }
}
