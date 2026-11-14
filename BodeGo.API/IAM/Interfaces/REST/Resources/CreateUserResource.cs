using System.ComponentModel.DataAnnotations;
namespace BodeGo.API.IAM.Interfaces.REST.Resources;

public sealed class CreateUserResource
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre no debe superar los 120 caracteres.")]
    public string Name { get; init; } = "";

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [StringLength(180, ErrorMessage = "El correo electrónico no debe superar los 180 caracteres.")]
    public string Email { get; init; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(12, ErrorMessage = "La contraseña debe tener al menos 12 caracteres.")]
    [MaxLength(128, ErrorMessage = "La contraseña no debe superar los 128 caracteres.")]
    public string Password { get; init; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "El identificador del rol debe ser un número entero positivo.")]
    public int RoleId { get; init; }
}
