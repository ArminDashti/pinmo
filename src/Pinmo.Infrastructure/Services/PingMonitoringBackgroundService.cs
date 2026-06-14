using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pinmo.Core.Entities;
using Pinmo.Core.Interfaces;
using Pinmo.Infrastructure.Data;

namespace Pinmo.Infrastructure.Services;

public class PingMonitoringBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<PingMonitoringBackgroundService> logger) : BackgroundService
{
    private readonly Dictionary<Guid, DateTime> _lastPingTimes = new();

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

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task RunMonitoringCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PinmoDbContext>();

        var settings = await dbContext.AppSettings.AsNoTracking().FirstAsync(cancellationToken);
        if (!settings.StartMonitoringOnLaunch)
        {
            return;
        }

        var endpoints = await dbContext.MonitoredEndpoints
            .Where(e => e.IsEnabled)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var endpoint in endpoints)
        {
            if (_lastPingTimes.TryGetValue(endpoint.Id, out var lastPing))
            {
                var elapsed = now - lastPing;
                if (elapsed.TotalSeconds < endpoint.IntervalSeconds)
                {
                    continue;
                }
            }

            var orchestrator = scope.ServiceProvider.GetRequiredService<IEndpointPingOrchestrator>();
            await orchestrator.PingAndRecordAsync(endpoint, cancellationToken);
            _lastPingTimes[endpoint.Id] = DateTime.UtcNow;
        }
    }
}
