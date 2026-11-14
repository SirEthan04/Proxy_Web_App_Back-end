using System.Globalization;
namespace BodeGo.API.Reporting.Interfaces.REST.Transform;
public static class ReportParameterParser
{
    public static DateOnly? Date(string? value)
    {
        if (value is null) return null;
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            throw new ArgumentException("Las fechas deben tener el formato yyyy-MM-dd.");
        return date;
    }
    public static int Limit(string? value)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var limit))
            throw new ArgumentException("El límite debe ser un número entero entre 1 y 100.");
        return limit;
    }
}
