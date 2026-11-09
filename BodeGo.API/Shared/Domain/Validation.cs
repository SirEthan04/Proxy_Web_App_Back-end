namespace BodeGo.API.Shared.Domain;

internal static class Validation
{
    public static string Text(string value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new ArgumentException($"Invalid {name}.", name);
        return value.Trim();
    }

    public static Guid Id(Guid value, string name)
    {
        if (value == Guid.Empty) throw new ArgumentException("An identifier is required.", name);
        return value;
    }

    public static Guid? OptionalId(Guid? value, string name) =>
        value.HasValue ? Id(value.Value, name) : null;

    public static int NonNegative(int value, string name)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }
}
