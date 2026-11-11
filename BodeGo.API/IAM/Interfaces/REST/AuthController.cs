using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Domain.Model.Commands;
using BodeGo.API.IAM.Interfaces.REST.Resources;
using BodeGo.API.IAM.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace BodeGo.API.IAM.Interfaces.REST;

[ApiController, Tags("IAM"), Route("api/v1/auth")]
public sealed class AuthController(IAuthenticationService authentication, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting(IamAuthenticationExtensions.LoginRateLimit)]
    [ProducesResponseType<LoginResult>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(429)]
    public async Task<IActionResult> Login(LoginResource resource, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await authentication.LoginAsync(new LoginCommand(resource.Email, resource.Password), ct);
        return result is null ? Problem(statusCode: 401, title: "El correo o la contraseña no son válidos.") : Ok(result);
    }

    [HttpGet("me"), Authorize]
    [ProducesResponseType<AuthenticatedUser>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var user = await authentication.GetCurrentUserAsync(new(currentUser.User.Id), ct);
        return user is null ? Problem(statusCode: 401, title: "Debe iniciar sesión con credenciales válidas.") : Ok(user);
    }
}
