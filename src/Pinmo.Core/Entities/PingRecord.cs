namespace Pinmo.Core.Entities;

public class PingRecord
{
    public Guid Id { get; set; }
    public Guid MonitoredEndpointId { get; set; }
    public MonitoredEndpoint MonitoredEndpoint { get; set; } = null!;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; }
    public int? StatusCode { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}
