using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain.Repositories;
using BodeGo.API.Operations.Infrastructure.Persistence.EFC;
using BodeGo.API.Operations.Infrastructure.Persistence.EFC.Repositories;
namespace BodeGo.API.Operations.Infrastructure;
public static class OperationsServiceCollectionExtensions
{
    public static IServiceCollection AddOperations(this IServiceCollection services)
    {
        services.AddScoped<BodeGo.API.Operations.Application.Services.IInventoryMovementQueryService, BodeGo.API.Operations.Application.Internal.QueryServices.InventoryMovementQueryService>();
        services.AddScoped<IOperationsRepository, OperationsRepository>();
        services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(BodeGo.API.Operations.Interfaces.REST.OperationsValidationResponse.Configure);
        services.AddScoped<IWasteCommandService, BodeGo.API.Operations.Application.Internal.CommandServices.WasteCommandService>();
        services.AddScoped<IOperationsSession, OperationsSession>();
        return services;
    }
}
