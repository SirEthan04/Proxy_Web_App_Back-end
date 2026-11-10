using BodeGo.API.Operations.Domain.Exceptions;
namespace BodeGo.API.Operations.Domain;
public static class OperationsRules
{
    public static void Id(Guid id)
    {
        if (id == Guid.Empty) throw new OperationsException("El identificador no puede estar vacío.");
    }
    public static void Lot(Guid? id) { if (id.HasValue) Id(id.Value); }
    public static void Text(string? text, int max, string field, bool optional = false)
    {
        if (optional && text == null) return;
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length > max)
            throw new OperationsException($"El campo {field} es obligatorio y admite hasta {max} caracteres.");
    }
}
