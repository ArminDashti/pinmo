namespace Pinmo.Infrastructure.Services;

public sealed class MonitoringScheduleState
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, DateTime> _lastPingTimes = new();

    public bool TryGetLastPingTime(Guid endpointId, out DateTime lastPingTime)
    {
        lock (_sync)
        {
            return _lastPingTimes.TryGetValue(endpointId, out lastPingTime);
        }
    }

    public void RecordPing(Guid endpointId, DateTime pingTime)
    {
        lock (_sync)
        {
            _lastPingTimes[endpointId] = pingTime;
        }
    }

    public void ResetSchedule()
    {
        lock (_sync)
        {
            _lastPingTimes.Clear();
        }
    }
}
