using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pinmo.Core.Interfaces;
using Pinmo.Infrastructure.Data;
using Pinmo.Infrastructure.Services;

namespace Pinmo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPinmoInfrastructure(
        this IServiceCollection services,
        string databasePath)
    {
        services.AddDbContext<PinmoDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        services.AddHttpClient("PingClient", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Pinmo/1.0");
        });

        services.AddScoped<IEndpointPingService, EndpointPingService>();
        services.AddScoped<IEndpointPingOrchestrator, EndpointPingOrchestrator>();
        services.AddHostedService<PingMonitoringBackgroundService>();

        return services;
    }
}
