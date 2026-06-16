using Pinmo.Core;

namespace Pinmo.Core.Entities;

public class AppSettings
{
    public int Id { get; set; } = 1;
    public int DefaultIntervalSeconds { get; set; } = MonitoringOptions.DefaultIntervalSeconds;
    public int DefaultPacketsPerPing { get; set; } = MonitoringOptions.DefaultPacketsPerPing;
    public int RequestTimeoutSeconds { get; set; } = 30;
}
