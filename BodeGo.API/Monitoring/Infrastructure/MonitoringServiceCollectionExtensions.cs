using BodeGo.API.Monitoring.Application.Internal.QueryServices;
using BodeGo.API.Monitoring.Application.Services;
using BodeGo.API.Monitoring.Domain.Repositories;
using BodeGo.API.Monitoring.Infrastructure.Persistence.EFC.Repositories;
namespace BodeGo.API.Monitoring.Infrastructure;
public static class MonitoringServiceCollectionExtensions
{
    public static IServiceCollection AddMonitoring(this IServiceCollection services)
    {
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IAlertQueryService, AlertQueryService>();
        return services;
    }
}
