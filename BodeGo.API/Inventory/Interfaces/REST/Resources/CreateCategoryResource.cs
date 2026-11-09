using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace BodeGo.API.Inventory.Interfaces.REST.Resources;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateCategoryResource
{
    [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre de la categoría no debe superar los 100 caracteres.")]
    public string Name { get; init; } = "";
    [StringLength(255, ErrorMessage = "La descripción no debe superar los 255 caracteres.")]
    public string? Description { get; init; }
}
