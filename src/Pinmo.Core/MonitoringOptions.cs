namespace Pinmo.Core;

public static class MonitoringOptions
{
    public static readonly int[] AllowedIntervalSeconds = [1, 5, 10, 15, 30, 45, 60];
    public static readonly int[] AllowedPacketsPerPing = [2, 4, 8, 16];

    public const int DefaultIntervalSeconds = 5;
    public const int DefaultPacketsPerPing = 2;

    public static bool IsValidInterval(int seconds) =>
        AllowedIntervalSeconds.Contains(seconds);

    public static bool IsValidPacketCount(int count) =>
        AllowedPacketsPerPing.Contains(count);

    public static int NormalizeInterval(int seconds) =>
        IsValidInterval(seconds) ? seconds : DefaultIntervalSeconds;

    public static int NormalizePacketCount(int count) =>
        IsValidPacketCount(count) ? count : DefaultPacketsPerPing;
}
