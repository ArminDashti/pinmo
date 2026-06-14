using Pinmo.Core.Dtos;
using Pinmo.Core.Entities;

namespace Pinmo.Api;

internal static class EndpointMapper
{
    public static EndpointResponse ToResponse(this MonitoredEndpoint endpoint) =>
        new(
            endpoint.Id,
            endpoint.Name,
            endpoint.Url,
            endpoint.HttpMethod,
            endpoint.IntervalSeconds,
            endpoint.PacketsPerPing,
            endpoint.IsEnabled,
            endpoint.CreatedAt,
            endpoint.LastCheckedAt,
            endpoint.LastStatusCode,
            endpoint.LastResponseTimeMs,
            endpoint.LastIsSuccess,
            endpoint.LastErrorMessage);

    public static SettingsResponse ToResponse(this AppSettings settings) =>
        new(
            settings.DefaultIntervalSeconds,
            settings.RequestTimeoutSeconds,
            settings.HistoryRetentionDays,
            settings.StartMonitoringOnLaunch,
            settings.NotifyOnFailure);
}
