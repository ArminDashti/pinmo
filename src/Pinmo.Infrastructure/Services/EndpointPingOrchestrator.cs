using Pinmo.Core.Dtos;

using Pinmo.Core.Entities;

using Pinmo.Core.Interfaces;

using Pinmo.Infrastructure.Data;



namespace Pinmo.Infrastructure.Services;



public class EndpointPingOrchestrator(

    IEndpointPingService pingService,

    IEndpointStore endpointStore,

    PinmoDbContext dbContext) : IEndpointPingOrchestrator

{

    public async Task<PingResultResponse> PingAndRecordAsync(

        MonitoredEndpoint endpoint,

        CancellationToken cancellationToken = default)

    {

        var result = await pingService.PingEndpointAsync(endpoint, cancellationToken);



        await endpointStore.UpdatePingStateAsync(

            endpoint.Id,

            result.CheckedAt,

            result.IsSuccess,

            result.StatusCode,

            result.PacketsSucceeded > 0 ? result.ResponseTimeMs : null,

            result.ErrorMessage,

            cancellationToken);



        endpoint.LastCheckedAt = result.CheckedAt;

        endpoint.LastIsSuccess = result.IsSuccess;

        endpoint.LastStatusCode = result.StatusCode;

        endpoint.LastResponseTimeMs = result.PacketsSucceeded > 0 ? result.ResponseTimeMs : null;

        endpoint.LastErrorMessage = result.ErrorMessage;



        dbContext.PingRecords.Add(new PingRecord

        {

            Id = Guid.NewGuid(),

            MonitoredEndpointId = endpoint.Id,

            CheckedAt = result.CheckedAt,

            IsSuccess = result.IsSuccess,

            StatusCode = result.StatusCode,

            ResponseTimeMs = result.ResponseTimeMs,

            PacketsSent = result.PacketsSent,

            PacketsSucceeded = result.PacketsSucceeded,

            ErrorMessage = result.ErrorMessage

        });



        await dbContext.SaveChangesAsync(cancellationToken);

        return result;

    }

}


