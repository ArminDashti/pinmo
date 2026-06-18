using Pinmo.Core.Entities;
using Pinmo.Core.Interfaces;

namespace Pinmo.Infrastructure.Storage;

public sealed class InMemoryPingRecordStore : IPingRecordStore
{
    private readonly List<PingRecord> _records = [];
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task AddAsync(PingRecord record, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _records.Add(record);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _records.Clear();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveForEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _records.RemoveAll(r => r.MonitoredEndpointId == endpointId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<PingRecord>> GetForEndpointsAsync(
        IEnumerable<Guid> endpointIds,
        CancellationToken cancellationToken = default)
    {
        var idSet = endpointIds.ToHashSet();

        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _records
                .Where(r => idSet.Contains(r.MonitoredEndpointId))
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }
}
