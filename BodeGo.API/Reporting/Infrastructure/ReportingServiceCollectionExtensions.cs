using BodeGo.API.Reporting.Application.Internal.QueryServices;
using BodeGo.API.Reporting.Application.Services;
using BodeGo.API.Reporting.Infrastructure.Persistence.EFC.Queries;
namespace BodeGo.API.Reporting.Infrastructure;
public static class ReportingServiceCollectionExtensions
{
    public static IServiceCollection AddReporting(this IServiceCollection services)
    {
        services.AddScoped<ISalesSummaryRepository, SalesSummaryRepository>();
        services.AddScoped<ISalesSummaryQueryService, SalesSummaryQueryService>();
        services.AddScoped<ITopProductsRepository, TopProductsRepository>();
        services.AddScoped<ITopProductsQueryService, TopProductsQueryService>();
        return services;
    }
}
