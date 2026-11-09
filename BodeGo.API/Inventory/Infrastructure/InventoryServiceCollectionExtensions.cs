using BodeGo.API.Inventory.Application.Internal.CommandServices;
using BodeGo.API.Inventory.Application.Internal.QueryServices;
using BodeGo.API.Inventory.Application.Services;
using BodeGo.API.Inventory.Domain.Repositories;
using BodeGo.API.Inventory.Infrastructure.Persistence.EFC.Repositories;
using BodeGo.API.Inventory.Interfaces.REST;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.Inventory.Infrastructure;
public static class InventoryServiceCollectionExtensions
{
    public static IServiceCollection AddInventory(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(InventoryValidationResponse.Configure);
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryCommandService, CategoryCommandService>();
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddScoped<IProductCommandService, ProductCommandService>();
        services.AddScoped<IProductQueryService, ProductQueryService>();
        return services;
    }
}
