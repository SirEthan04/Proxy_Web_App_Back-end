using System.Security.Claims;
using BodeGo.API.IAM.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
namespace BodeGo.API.IAM.Infrastructure.Authentication;

public sealed class DatabaseJwtEvents(IAuthenticationService authentication) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirst("sub")?.Value, out var id) || id == Guid.Empty)
        { context.Fail("Identidad no válida."); return; }
        var user = await authentication.GetCurrentUserAsync(new(id), context.HttpContext.RequestAborted);
        if (user is null) { context.Fail("Identidad no válida."); return; }
        // El rol proviene de MySQL en esta petición, nunca de un token antiguo.
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", user.Id.ToString()), new Claim("role", user.Role)],
            "Bearer", "sub", "role"));
        context.HttpContext.Items[HttpCurrentUser.ItemKey] = user;
    }

    public override async Task Challenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        context.Response.Headers.WWWAuthenticate = "Bearer";
        await Results.Problem(statusCode: 401, title: "Debe iniciar sesión con credenciales válidas.")
            .ExecuteAsync(context.HttpContext);
    }

    public override Task Forbidden(ForbiddenContext context) =>
        Results.Problem(statusCode: 403, title: "No tiene permiso para realizar esta operación.")
            .ExecuteAsync(context.HttpContext);
}
