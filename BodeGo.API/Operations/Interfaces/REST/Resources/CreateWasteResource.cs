using System.ComponentModel.DataAnnotations;
namespace BodeGo.API.Operations.Interfaces.REST.Resources;
public record CreateWasteResource(
    [Required(ErrorMessage = "El producto es obligatorio.")] Guid? ProductId,
    Guid? LotId,
    [Required(ErrorMessage = "El usuario es obligatorio.")] Guid? UserId,
    [Required(ErrorMessage = "La cantidad es obligatoria.")] int? Quantity,
    [Required(ErrorMessage = "El motivo es obligatorio.")] string Reason);
