using BodeGo.API.IAM.Application.Model.Results;
using BodeGo.API.IAM.Application.Services;
namespace BodeGo.API.IAM.Infrastructure.Authentication;
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    internal const string ItemKey = "BodeGo.AuthenticatedUser";
    public AuthenticatedUser User => accessor.HttpContext?.Items[ItemKey] as AuthenticatedUser
        ?? throw new InvalidOperationException("No existe una identidad autenticada validada.");
}
