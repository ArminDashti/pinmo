using Pinmo.Core.Dtos;
using Pinmo.Core.Entities;

namespace Pinmo.Core.Interfaces;

public interface IEndpointPingService
{
    Task<PingResultResponse> PingEndpointAsync(MonitoredEndpoint endpoint, CancellationToken cancellationToken = default);
}
