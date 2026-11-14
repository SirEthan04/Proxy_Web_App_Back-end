namespace BodeGo.API.IAM.Application.Services;
public interface IPasswordHashService
{
    bool Verify(string? hash, string password);
    string Hash(string password);
}
