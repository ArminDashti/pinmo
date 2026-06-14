namespace Pinmo.Core.Dtos;

public record EndpointRequest(
    string Name,
    string Url,
    string HttpMethod = "GET",
    int IntervalSeconds = 60,
    int PacketsPerPing = 2,
    bool IsEnabled = true);

public record EndpointResponse(
    Guid Id,
    string Name,
    string Url,
    string HttpMethod,
    int IntervalSeconds,
    int PacketsPerPing,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime? LastCheckedAt,
    int? LastStatusCode,
    int? LastResponseTimeMs,
    bool? LastIsSuccess,
    string? LastErrorMessage);

public record PingRecordResponse(
    Guid Id,
    Guid MonitoredEndpointId,
    string EndpointName,
    string EndpointUrl,
    DateTime CheckedAt,
    bool IsSuccess,
    int? StatusCode,
    int ResponseTimeMs,
    string? ErrorMessage);

public record DashboardSummary(
    int TotalEndpoints,
    int EnabledEndpoints,
    int UpCount,
    int DownCount,
    int UnknownCount,
    double AverageResponseTimeMs,
    IReadOnlyList<EndpointResponse> Endpoints);

public record SettingsRequest(
    int DefaultIntervalSeconds,
    int RequestTimeoutSeconds,
    int HistoryRetentionDays,
    bool StartMonitoringOnLaunch,
    bool NotifyOnFailure);

public record SettingsResponse(
    int DefaultIntervalSeconds,
    int RequestTimeoutSeconds,
    int HistoryRetentionDays,
    bool StartMonitoringOnLaunch,
    bool NotifyOnFailure);

public record PingResultResponse(
    Guid EndpointId,
    bool IsSuccess,
    int? StatusCode,
    int ResponseTimeMs,
    string? ErrorMessage,
    DateTime CheckedAt);
