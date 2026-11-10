using System.ComponentModel.DataAnnotations;
namespace BodeGo.API.Operations.Interfaces.REST.Resources;
public record CreateWasteResource(
    [property: Required(ErrorMessage = "El producto es obligatorio.")] Guid? ProductId,
    Guid? LotId,
    [property: Required(ErrorMessage = "El usuario es obligatorio.")] Guid? UserId,
    [property: Required(ErrorMessage = "La cantidad es obligatoria.")] int? Quantity,
    [property: Required(ErrorMessage = "El motivo es obligatorio.")] string Reason);
