using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace BodeGo.API.Inventory.Interfaces.REST.Resources;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateProductResource
{
    [Required(ErrorMessage = "El identificador de la categoría es obligatorio.")]
    public Guid? CategoryId { get; init; }
    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre del producto no debe superar los 150 caracteres.")]
    public string Name { get; init; } = "";
    [Required(ErrorMessage = "El precio de venta es obligatorio.")]
    [Range(typeof(decimal), "0", "9999999999.99", ErrorMessage = "El precio debe estar entre 0 y 9999999999.99.")]
    public decimal? SalePrice { get; init; }
    [Required(ErrorMessage = "El stock mínimo es obligatorio.")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo debe ser un número entero no negativo.")]
    public int? MinimumStock { get; init; }
}
