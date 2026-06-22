namespace Pinmo.Core.Dtos;

public record EndpointRequest(string Url);

public record EndpointResponse(
    Guid Id,
    string Url,
    DateTime CreatedAt,
    DateTime? LastCheckedAt,
    int? LastResponseTimeMs,
    bool? LastIsSuccess,
    string? LastErrorMessage);

public record DashboardEndpointRow(
    Guid Id,
    string Url,
    int? LatestPingMs,
    bool LatestPingIsTimeout,
    double? AvgPingMs,
    bool AvgPingIsTimeout,
    double? AvgPacketLossPercent,
    int? TotalPacketsSent);

public record DashboardSummary(
    IReadOnlyList<DashboardEndpointRow> Endpoints,
    int FailedPingCount);

public record PingResultResponse(
    Guid EndpointId,
    bool IsSuccess,
    int? StatusCode,
    int ResponseTimeMs,
    string? ErrorMessage,
    DateTime CheckedAt,
    int PacketsSent,
    int PacketsSucceeded);
