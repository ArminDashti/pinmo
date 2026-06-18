namespace Pinmo.Core.Entities;

public class MonitoredEndpoint
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "GET";
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastCheckedAt { get; set; }
    public int? LastStatusCode { get; set; }
    public int? LastResponseTimeMs { get; set; }
    public bool? LastIsSuccess { get; set; }
    public string? LastErrorMessage { get; set; }
}
