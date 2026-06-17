using Pinmo.Core.Entities;

namespace Pinmo.Core.Interfaces;

public interface IEndpointStore
{
    Task<IReadOnlyList<MonitoredEndpoint>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MonitoredEndpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MonitoredEndpoint> AddAsync(MonitoredEndpoint endpoint, CancellationToken cancellationToken = default);
    Task<MonitoredEndpoint?> UpdateAsync(MonitoredEndpoint endpoint, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdatePingStateAsync(
        Guid id,
        DateTime checkedAt,
        bool isSuccess,
        int? statusCode,
        int? responseTimeMs,
        string? errorMessage,
        CancellationToken cancellationToken = default);
    Task ResetAllPingStateAsync(CancellationToken cancellationToken = default);
}
