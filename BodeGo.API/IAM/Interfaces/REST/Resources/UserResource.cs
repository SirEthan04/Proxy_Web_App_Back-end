namespace BodeGo.API.IAM.Interfaces.REST.Resources;
public sealed record UserResource(Guid Id, string Name, string Email, int RoleId, bool Active, DateTime CreatedAt);
