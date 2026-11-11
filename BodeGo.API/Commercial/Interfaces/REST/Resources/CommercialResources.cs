using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace BodeGo.API.Commercial.Interfaces.REST.Resources;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CommercialItemResource
{
    [Required(ErrorMessage="El identificador del producto es obligatorio.")] public Guid? ProductId { get; init; }
    public Guid? LotId { get; init; }
    [Required(ErrorMessage="La cantidad es obligatoria.")][Range(1,int.MaxValue,ErrorMessage="La cantidad debe ser mayor que cero.")] public int? Quantity { get; init; }
    [Required(ErrorMessage="El importe unitario es obligatorio.")][Range(typeof(decimal),"0","9999999999.99",ErrorMessage="El importe debe ser válido y tener hasta dos decimales.")] public decimal? UnitAmount { get; init; }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateCommercialResource
{
    [Required(ErrorMessage="Debe incluir los detalles.")][MinLength(1,ErrorMessage="Debe incluir al menos un detalle.")] public List<CommercialItemResource> Items { get; init; } = [];
}
public sealed record CommercialItemResponse(Guid Id, Guid ProductId, Guid? LotId, int Quantity, decimal UnitAmount, decimal Subtotal);
public sealed record CommercialResponse(Guid Id, Guid UserId, DateTime RegisteredAt, string Status, decimal Total, IReadOnlyList<CommercialItemResponse> Items);
