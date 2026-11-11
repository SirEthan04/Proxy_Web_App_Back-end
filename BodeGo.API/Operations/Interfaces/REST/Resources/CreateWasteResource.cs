using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace BodeGo.API.Operations.Interfaces.REST.Resources;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreateWasteResource(
    [Required(ErrorMessage = "El producto es obligatorio.")] Guid? ProductId,
    Guid? LotId,
    [Required(ErrorMessage = "La cantidad es obligatoria.")] int? Quantity,
    [Required(ErrorMessage = "El motivo es obligatorio.")] string Reason);
