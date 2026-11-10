using System.ComponentModel.DataAnnotations;
namespace BodeGo.API.Operations.Interfaces.REST.Resources;
public record CreateCountItemResource(
    [Required(ErrorMessage = "El producto es obligatorio.")] Guid? ProductId,
    Guid? LotId,
    [Required(ErrorMessage = "La cantidad física es obligatoria.")] int? CountedQuantity);
public record CreateInventoryCountResource(
    [Required(ErrorMessage = "El usuario es obligatorio.")] Guid? UserId,
    string? Notes,
    [Required(ErrorMessage = "Los detalles son obligatorios.")] IReadOnlyList<CreateCountItemResource> Items);
