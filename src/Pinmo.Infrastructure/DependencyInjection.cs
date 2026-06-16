using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pinmo.Core.Interfaces;
using Pinmo.Infrastructure.Data;
using Pinmo.Infrastructure.Services;
using Pinmo.Infrastructure.Storage;

namespace Pinmo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPinmoInfrastructure(
        this IServiceCollection services,
        string databasePath,
        string appDataPath)
    {
        Directory.CreateDirectory(appDataPath);

        var endpointsPath = Path.Combine(appDataPath, "endpoints.json");
        var settingsPath = Path.Combine(appDataPath, "settings.json");

        services.AddDbContext<PinmoDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton(new JsonEndpointStore(endpointsPath));
        services.AddSingleton<IEndpointStore>(sp => sp.GetRequiredService<JsonEndpointStore>());
        services.AddSingleton(new JsonSettingsStore(settingsPath));
        services.AddSingleton<ISettingsStore>(sp => sp.GetRequiredService<JsonSettingsStore>());

        services.AddHttpClient("PingClient", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Pinmo/1.0");
        });

        services.AddSingleton<MonitoringScheduleState>();

        services.AddScoped<IEndpointPingService, EndpointPingService>();
        services.AddScoped<IEndpointPingOrchestrator, EndpointPingOrchestrator>();
        services.AddHostedService<PingMonitoringBackgroundService>();

        return services;
    }
}
