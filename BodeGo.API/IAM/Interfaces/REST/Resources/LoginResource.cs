using System.ComponentModel.DataAnnotations;
namespace BodeGo.API.IAM.Interfaces.REST.Resources;
public sealed record LoginResource(
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [StringLength(180, ErrorMessage = "El correo electrónico no debe superar los 180 caracteres.")] string Email,
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(128, ErrorMessage = "La contraseña no debe superar los 128 caracteres.")] string Password);
