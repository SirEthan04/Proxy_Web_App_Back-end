using BodeGo.API.Commercial.Application.Internal.CommandServices;
using BodeGo.API.Commercial.Application.Internal.QueryServices;
using BodeGo.API.Commercial.Application.Services;
using BodeGo.API.Commercial.Domain.Repositories;
using BodeGo.API.Commercial.Infrastructure.Persistence.EFC.Repositories;
namespace BodeGo.API.Commercial.Infrastructure;
public static class CommercialServiceCollectionExtensions
{
    public static IServiceCollection AddCommercial(this IServiceCollection services)
    {
        services.AddScoped<IPurchaseRepository,PurchaseRepository>(); services.AddScoped<ISaleRepository,SaleRepository>();
        services.AddScoped<PurchaseCommandService>(); services.AddScoped<IPurchaseCommandService,PurchaseCommandService>(); services.AddScoped<ISaleCommandService,SaleCommandService>();
        services.AddScoped<IPurchaseQueryService,PurchaseQueryService>(); services.AddScoped<ISaleQueryService,SaleQueryService>(); return services;
    }
}
