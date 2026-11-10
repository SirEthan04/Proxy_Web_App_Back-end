using System.ComponentModel.DataAnnotations;
namespace BodeGo.API.Operations.Interfaces.REST.Resources;
public record CreateCountItemResource(
    [property: Required(ErrorMessage = "El producto es obligatorio.")] Guid? ProductId,
    Guid? LotId,
    [property: Required(ErrorMessage = "La cantidad física es obligatoria.")] int? CountedQuantity);
public record CreateInventoryCountResource(
    [property: Required(ErrorMessage = "El usuario es obligatorio.")] Guid? UserId,
    string? Notes,
    [property: Required(ErrorMessage = "Los detalles son obligatorios.")] IReadOnlyList<CreateCountItemResource> Items);
