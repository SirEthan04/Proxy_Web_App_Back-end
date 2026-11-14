namespace BodeGo.API.Commercial.Domain.Model.ValueObjects;
public sealed record Quantity
{
    public int Value { get; }
    public Quantity(int value)
    {
        if (value < 1) throw new ArgumentOutOfRangeException(nameof(value));
        Value = value;
    }
}
