namespace BodeGo.API.Inventory.Domain;
internal static class InventoryValidation
{
    public static string Text(string value, int maximum, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"El {field} es obligatorio.");
        if (value.Trim().Length > maximum) throw new ArgumentException($"El {field} no debe superar los {maximum} caracteres.");
        return value.Trim();
    }
    public static Guid Id(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("El identificador no puede estar vacío.");
        return value;
    }
}
