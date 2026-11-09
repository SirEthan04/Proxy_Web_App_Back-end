using System.ComponentModel.DataAnnotations;
namespace BodeGo.API.IAM.Interfaces.REST.Resources;

public sealed class CreateUserResource
{
    [Required, StringLength(120)] public string Name { get; init; } = "";
    [Required, StringLength(180)] public string Email { get; init; } = "";
    [Required, StringLength(128, MinimumLength = 12)] public string Password { get; init; } = "";
    [Range(1, int.MaxValue)] public int RoleId { get; init; }
}
