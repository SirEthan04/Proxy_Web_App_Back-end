namespace BodeGo.API.IAM.Application.Services;
public interface IPasswordHashService
{
    string Hash(string password);
}
