namespace BodeGo.API.Commercial.Domain.Model.ValueObjects;

public sealed record Money
{
    public decimal Value { get; }
    public Money(decimal value)
    {
        if (value < 0 || value > 9999999999.99m || decimal.Round(value, 2) != value)
            throw new ArgumentOutOfRangeException(nameof(value), "Use a non-negative amount with at most two decimals.");
        Value = value;
    }
}
