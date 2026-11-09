using BodeGo.API.IAM.Application.Services;
using Microsoft.AspNetCore.Identity;
namespace BodeGo.API.IAM.Infrastructure.Hashing;

public class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<object> _hasher = new();
    public string Hash(string password) => _hasher.HashPassword(new object(), password);
}
