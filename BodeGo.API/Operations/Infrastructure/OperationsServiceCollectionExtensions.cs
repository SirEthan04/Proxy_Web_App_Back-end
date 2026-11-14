using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Application.Internal.CommandServices;
using BodeGo.API.Operations.Application.Internal.QueryServices;
using BodeGo.API.Operations.Interfaces.REST;
using Microsoft.AspNetCore.Mvc;
using BodeGo.API.Operations.Domain.Repositories;
using BodeGo.API.Operations.Infrastructure.Persistence.EFC;
using BodeGo.API.Operations.Infrastructure.Persistence.EFC.Repositories;
namespace BodeGo.API.Operations.Infrastructure;
public static class OperationsServiceCollectionExtensions
{
    public static IServiceCollection AddOperations(this IServiceCollection services)
    {
        services.AddScoped<IInventoryMovementQueryService, InventoryMovementQueryService>();
        services.AddScoped<IOperationsRepository, OperationsRepository>();
        services.Configure<ApiBehaviorOptions>(OperationsValidationResponse.Configure);
        services.AddScoped<IWasteCommandService, WasteCommandService>();
        services.AddScoped<IOperationsSession, OperationsSession>();
        services.AddScoped<IInventoryCountCommandService, InventoryCountCommandService>();
        services.AddScoped<IInventoryCountQueryService, InventoryCountQueryService>();
        services.AddScoped<IWasteQueryService, WasteQueryService>();
        return services;
    }
}
