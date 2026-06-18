namespace Pinmo.Core.Entities;

public class PingRecord
{
    public Guid Id { get; set; }
    public Guid MonitoredEndpointId { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; }
    public int? StatusCode { get; set; }
    public int ResponseTimeMs { get; set; }
    public int PacketsSent { get; set; } = 1;
    public int PacketsSucceeded { get; set; }
    public string? ErrorMessage { get; set; }
}
