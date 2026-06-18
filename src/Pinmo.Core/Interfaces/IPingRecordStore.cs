using Pinmo.Core.Entities;

namespace Pinmo.Core.Interfaces;

public interface IPingRecordStore
{
    Task AddAsync(PingRecord record, CancellationToken cancellationToken = default);
    Task ClearAllAsync(CancellationToken cancellationToken = default);
    Task RemoveForEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PingRecord>> GetForEndpointsAsync(
        IEnumerable<Guid> endpointIds,
        CancellationToken cancellationToken = default);
}
