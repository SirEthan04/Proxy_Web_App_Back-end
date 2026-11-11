using Microsoft.IdentityModel.Tokens;
namespace BodeGo.API.IAM.Infrastructure.Authentication;

public sealed class JwtSettings
{
    public string Issuer { get; set; } = "BodeGo.API";
    public string Audience { get; set; } = "BodeGo.Vue";
    public string SigningKeyBase64 { get; set; } = "";
    public int LifetimeMinutes { get; set; } = 15;

    public bool IsValid()
    {
        try { return !string.IsNullOrWhiteSpace(Issuer) && !string.IsNullOrWhiteSpace(Audience)
            && LifetimeMinutes is >= 1 and <= 30 && Convert.FromBase64String(SigningKeyBase64).Length >= 32; }
        catch (FormatException) { return false; }
    }

    public SymmetricSecurityKey Key() => new(Convert.FromBase64String(SigningKeyBase64));
    public TokenValidationParameters ValidationParameters() => new()
    {
        ValidateIssuerSigningKey = true, IssuerSigningKey = Key(),
        ValidateIssuer = true, ValidIssuer = Issuer,
        ValidateAudience = true, ValidAudience = Audience,
        ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.FromSeconds(15),
        NameClaimType = "sub", RoleClaimType = "role"
    };
}
