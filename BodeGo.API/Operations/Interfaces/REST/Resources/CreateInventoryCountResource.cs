using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace BodeGo.API.Operations.Interfaces.REST.Resources;
public record CreateCountItemResource(
    [Required(ErrorMessage = "El producto es obligatorio.")] Guid? ProductId,
    Guid? LotId,
    [Required(ErrorMessage = "La cantidad física es obligatoria.")] int? CountedQuantity);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreateInventoryCountResource(
    string? Notes,
    [Required(ErrorMessage = "Los detalles son obligatorios.")] IReadOnlyList<CreateCountItemResource> Items);
