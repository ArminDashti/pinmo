using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pinmo.Core;
using Pinmo.Core.Entities;
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
            var cycleStarted = DateTime.UtcNow;

            try
            {
                await RunMonitoringCycleAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Monitoring cycle failed.");
            }

            var elapsed = DateTime.UtcNow - cycleStarted;
            var delay = TimeSpan.FromSeconds(MonitoringOptions.IntervalSeconds) - elapsed;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private async Task RunMonitoringCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var endpointStore = scope.ServiceProvider.GetRequiredService<IEndpointStore>();

        var endpoints = (await endpointStore.GetAllAsync(cancellationToken))
            .Where(e => e.IsEnabled)
            .ToList();

        var now = DateTime.UtcNow;
        var dueEndpoints = endpoints
            .Where(endpoint => IsEndpointDue(endpoint.Id, now))
            .ToList();

        if (dueEndpoints.Count == 0)
        {
            return;
        }

        var pingTasks = dueEndpoints
            .Select(endpoint => PingEndpointInScopeAsync(endpoint, cancellationToken))
            .ToList();

        await Task.WhenAll(pingTasks);
    }

    private bool IsEndpointDue(Guid endpointId, DateTime now)
    {
        if (!scheduleState.TryGetLastPingTime(endpointId, out var lastPing))
        {
            return true;
        }

        return (now - lastPing).TotalSeconds >= MonitoringOptions.IntervalSeconds;
    }

    private async Task PingEndpointInScopeAsync(MonitoredEndpoint endpoint, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<IEndpointPingOrchestrator>();
            await orchestrator.PingAndRecordAsync(endpoint, cancellationToken);
            scheduleState.RecordPing(endpoint.Id, DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Ping failed for endpoint {EndpointId} ({Url}).", endpoint.Id, endpoint.Url);
        }
    }
}
