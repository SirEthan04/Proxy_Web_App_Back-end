using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Application.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
namespace BodeGo.API.IAM.Infrastructure.Authentication;

public sealed class JwtAccessTokenService(IOptions<JwtSettings> settings) : IAccessTokenService
{
    public LoginResult Issue(AuthenticatedUser user)
    {
        var options = settings.Value;
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(options.LifetimeMinutes);
        var token = new JwtSecurityToken(options.Issuer, options.Audience,
            [new Claim("sub", user.Id.ToString()), new Claim("jti", Guid.NewGuid().ToString())],
            now, expires, new SigningCredentials(options.Key(), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", expires, user);
    }
}
