using BodeGo.API.IAM.Application.Services;
using Microsoft.AspNetCore.Identity;
namespace BodeGo.API.IAM.Infrastructure.Hashing;

public class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<object> _hasher = new();
    public string Hash(string password) => _hasher.HashPassword(new object(), password);
    private static readonly string DummyHash = new PasswordHasher<object>()
        .HashPassword(new object(), Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
    public bool Verify(string? hash, string password)
    {
        try { return _hasher.VerifyHashedPassword(new object(), hash ?? DummyHash, password) != PasswordVerificationResult.Failed && hash is not null; }
        catch (FormatException) { return false; }
    }
}
