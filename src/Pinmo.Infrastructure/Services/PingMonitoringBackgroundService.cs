using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pinmo.Core;
using Pinmo.Core.Interfaces;

namespace Pinmo.Infrastructure.Services;

public class PingMonitoringBackgroundService(
    IServiceScopeFactory scopeFactory,
    MonitoringScheduleState scheduleState,
    ILogger<PingMonitoringBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Ping monitoring service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunMonitoringCycleAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Monitoring cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task RunMonitoringCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var settingsStore = scope.ServiceProvider.GetRequiredService<ISettingsStore>();
        var endpointStore = scope.ServiceProvider.GetRequiredService<IEndpointStore>();

        var settings = await settingsStore.GetAsync(cancellationToken);
        var endpoints = (await endpointStore.GetAllAsync(cancellationToken))
            .Where(e => e.IsEnabled)
            .ToList();

        var now = DateTime.UtcNow;
        var intervalSeconds = MonitoringOptions.NormalizeInterval(settings.DefaultIntervalSeconds);

        foreach (var endpoint in endpoints)
        {
            if (scheduleState.TryGetLastPingTime(endpoint.Id, out var lastPing))
            {
                var elapsed = now - lastPing;
                if (elapsed.TotalSeconds < intervalSeconds)
                {
                    continue;
                }
            }

            var orchestrator = scope.ServiceProvider.GetRequiredService<IEndpointPingOrchestrator>();
            await orchestrator.PingAndRecordAsync(endpoint, cancellationToken);
            scheduleState.RecordPing(endpoint.Id, DateTime.UtcNow);
        }
    }
}
