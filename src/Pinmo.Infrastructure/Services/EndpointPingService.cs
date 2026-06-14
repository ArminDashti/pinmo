using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Pinmo.Core;
using Pinmo.Core.Dtos;
using Pinmo.Core.Entities;
using Pinmo.Core.Interfaces;
using Pinmo.Infrastructure.Data;

namespace Pinmo.Infrastructure.Services;

public class EndpointPingService(
    IHttpClientFactory httpClientFactory,
    PinmoDbContext dbContext) : IEndpointPingService
{
    public async Task<PingResultResponse> PingEndpointAsync(
        MonitoredEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.AppSettings.AsNoTracking().FirstAsync(cancellationToken);
        var client = httpClientFactory.CreateClient("PingClient");
        client.Timeout = TimeSpan.FromSeconds(settings.RequestTimeoutSeconds);

        var packetCount = MonitoringOptions.NormalizePacketCount(endpoint.PacketsPerPing);
        var attempts = new List<PingAttemptResult>(packetCount);

        for (var i = 0; i < packetCount; i++)
        {
            attempts.Add(await SendSinglePingAsync(client, endpoint, cancellationToken));
        }

        return AggregateResults(endpoint.Id, attempts);
    }

    private static async Task<PingAttemptResult> SendSinglePingAsync(
        HttpClient client,
        MonitoredEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(endpoint.HttpMethod), endpoint.Url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            using var response = await client.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            var isSuccess = response.IsSuccessStatusCode;
            var statusCode = (int)response.StatusCode;

            return new PingAttemptResult(
                isSuccess,
                statusCode,
                (int)stopwatch.ElapsedMilliseconds,
                isSuccess ? null : $"HTTP {statusCode} {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new PingAttemptResult(false, null, (int)stopwatch.ElapsedMilliseconds, ex.Message);
        }
    }

    private static PingResultResponse AggregateResults(Guid endpointId, IReadOnlyList<PingAttemptResult> attempts)
    {
        var checkedAt = DateTime.UtcNow;
        var successCount = attempts.Count(a => a.IsSuccess);
        var allSuccess = successCount == attempts.Count;
        var averageResponseTime = (int)Math.Round(attempts.Average(a => a.ResponseTimeMs));

        var statusCode = attempts.LastOrDefault(a => a.StatusCode.HasValue)?.StatusCode;
        string? errorMessage = null;

        if (!allSuccess)
        {
            var firstFailure = attempts.FirstOrDefault(a => !a.IsSuccess);
            statusCode ??= firstFailure?.StatusCode;
            errorMessage = $"{successCount}/{attempts.Count} packets succeeded";
            if (firstFailure?.ErrorMessage is not null)
            {
                errorMessage += $": {firstFailure.ErrorMessage}";
            }
        }

        return new PingResultResponse(
            endpointId,
            allSuccess,
            statusCode,
            averageResponseTime,
            errorMessage,
            checkedAt);
    }

    private sealed record PingAttemptResult(
        bool IsSuccess,
        int? StatusCode,
        int ResponseTimeMs,
        string? ErrorMessage);
}
