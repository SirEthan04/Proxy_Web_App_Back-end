namespace BodeGo.API.IAM.Application.Model.Results;
public sealed record LoginResult(string AccessToken, string TokenType, DateTime ExpiresAt, AuthenticatedUser User);
