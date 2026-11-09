using BodeGo.API.IAM.Application.Internal.CommandServices;
using BodeGo.API.IAM.Application.Internal.QueryServices;
using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Domain.Repositories;
using BodeGo.API.IAM.Infrastructure.Hashing;
using BodeGo.API.IAM.Infrastructure.Persistence.EFC.Repositories;
namespace BodeGo.API.IAM.Infrastructure;

public static class IamServiceCollectionExtensions
{
    public static IServiceCollection AddIam(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IUserCommandService, UserCommandService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        return services;
    }
}
