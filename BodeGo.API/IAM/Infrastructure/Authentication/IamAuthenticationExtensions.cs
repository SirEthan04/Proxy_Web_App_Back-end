using System.Threading.RateLimiting;
using BodeGo.API.IAM.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
namespace BodeGo.API.IAM.Infrastructure.Authentication;

public static class IamAuthenticationExtensions
{
    public const string Administrator = "IamAdministrator";
    public const string Commercial = "CommercialOperator";
    public const string Operations = "OperationsOperator";
    public const string LoginRateLimit = "IamLogin";

    public static IServiceCollection AddIamAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>().Bind(configuration.GetSection("Jwt"))
            .Validate(o => o.IsValid(), "Configure Jwt:SigningKeyBase64 con al menos 32 bytes aleatorios; duración entre 1 y 30 minutos.")
            .ValidateOnStart();
        services.AddScoped<IAccessTokenService, JwtAccessTokenService>();
        services.AddScoped<IAuthenticationService, Application.Internal.CommandServices.AuthenticationService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<DatabaseJwtEvents>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, settings) =>
            {
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = false;
                options.SaveToken = false;
                options.TokenValidationParameters = settings.Value.ValidationParameters();
                options.EventsType = typeof(DatabaseJwtEvents);
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Administrator, p => p.RequireAuthenticatedUser().RequireRole("Administrador"));
            options.AddPolicy(Commercial, p => p.RequireAuthenticatedUser().RequireRole("Administrador", "Empleado"));
            options.AddPolicy(Operations, p => p.RequireAuthenticatedUser().RequireRole("Administrador", "Empleado"));
        });
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(LoginRateLimit, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retry.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                await Results.Problem(statusCode: 429, title: "Demasiados intentos. Inténtelo de nuevo en un minuto.")
                    .ExecuteAsync(context.HttpContext);
            };
        });
        return services;
    }
}
