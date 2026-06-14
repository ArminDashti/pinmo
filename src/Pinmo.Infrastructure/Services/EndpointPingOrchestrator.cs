using Pinmo.Core.Dtos;
using Pinmo.Core.Entities;
using Pinmo.Core.Interfaces;
using Pinmo.Infrastructure.Data;

namespace Pinmo.Infrastructure.Services;

public class EndpointPingOrchestrator(
    IEndpointPingService pingService,
    PinmoDbContext dbContext) : IEndpointPingOrchestrator
{
    public async Task<PingResultResponse> PingAndRecordAsync(
        MonitoredEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        var result = await pingService.PingEndpointAsync(endpoint, cancellationToken);

        endpoint.LastCheckedAt = result.CheckedAt;
        endpoint.LastIsSuccess = result.IsSuccess;
        endpoint.LastStatusCode = result.StatusCode;
        endpoint.LastResponseTimeMs = result.ResponseTimeMs;
        endpoint.LastErrorMessage = result.ErrorMessage;

        dbContext.PingRecords.Add(new PingRecord
        {
            Id = Guid.NewGuid(),
            MonitoredEndpointId = endpoint.Id,
            CheckedAt = result.CheckedAt,
            IsSuccess = result.IsSuccess,
            StatusCode = result.StatusCode,
            ResponseTimeMs = result.ResponseTimeMs,
            ErrorMessage = result.ErrorMessage
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }
}
