using Pinmo.Core.Dtos;
using Pinmo.Core.Entities;

namespace Pinmo.Core.Interfaces;

public interface IEndpointPingOrchestrator
{
    Task<PingResultResponse> PingAndRecordAsync(MonitoredEndpoint endpoint, CancellationToken cancellationToken = default);
}
