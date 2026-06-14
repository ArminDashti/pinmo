namespace Pinmo.Core.Entities;

public class AppSettings
{
    public int Id { get; set; } = 1;
    public int DefaultIntervalSeconds { get; set; } = 60;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int HistoryRetentionDays { get; set; } = 30;
    public bool StartMonitoringOnLaunch { get; set; } = true;
    public bool NotifyOnFailure { get; set; } = true;
}
