using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain.Repositories;
using BodeGo.API.Operations.Infrastructure.Persistence.EFC;
using BodeGo.API.Operations.Infrastructure.Persistence.EFC.Repositories;
namespace BodeGo.API.Operations.Infrastructure;
public static class OperationsServiceCollectionExtensions
{
    public static IServiceCollection AddOperations(this IServiceCollection services)
    {
        services.AddScoped<IOperationsRepository, OperationsRepository>();
        services.AddScoped<IOperationsSession, OperationsSession>();
        return services;
    }
}
