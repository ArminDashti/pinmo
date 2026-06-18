using Microsoft.Extensions.DependencyInjection;
using Pinmo.Core.Interfaces;
using Pinmo.Infrastructure.Services;
using Pinmo.Infrastructure.Storage;

namespace Pinmo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPinmoInfrastructure(
        this IServiceCollection services,
        string appDataPath)
    {
        services.AddSingleton<InMemoryEndpointStore>();
        services.AddSingleton<IEndpointStore>(sp => sp.GetRequiredService<InMemoryEndpointStore>());
        services.AddSingleton<InMemorySettingsStore>();
        services.AddSingleton<ISettingsStore>(sp => sp.GetRequiredService<InMemorySettingsStore>());
        services.AddSingleton<InMemoryPingRecordStore>();
        services.AddSingleton<IPingRecordStore>(sp => sp.GetRequiredService<InMemoryPingRecordStore>());

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

    public static string GetEndpointsSeedPath(string appDataPath) =>
        Path.Combine(appDataPath, "endpoints.json");

    public static string GetSettingsSeedPath(string appDataPath) =>
        Path.Combine(appDataPath, "settings.json");
}
