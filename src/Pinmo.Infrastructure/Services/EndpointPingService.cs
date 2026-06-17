using System.Diagnostics;

using System.Net.Http.Headers;

using System.Net.NetworkInformation;

using Pinmo.Core;

using Pinmo.Core.Dtos;

using Pinmo.Core.Entities;

using Pinmo.Core.Interfaces;



namespace Pinmo.Infrastructure.Services;



public class EndpointPingService(

    IHttpClientFactory httpClientFactory,

    ISettingsStore settingsStore) : IEndpointPingService

{

    public async Task<PingResultResponse> PingEndpointAsync(

        MonitoredEndpoint endpoint,

        CancellationToken cancellationToken = default)

    {

        var settings = await settingsStore.GetAsync(cancellationToken);

        var packetCount = MonitoringOptions.PacketsPerPing;

        var timeoutMs = Math.Clamp(settings.RequestTimeoutSeconds, 1, 120) * 1000;

        var attempts = new List<PingAttemptResult>(packetCount);



        if (EndpointAddress.ShouldUseIcmpPing(endpoint.Url))

        {

            var host = EndpointAddress.GetPingHost(endpoint.Url);



            for (var i = 0; i < packetCount; i++)

            {

                attempts.Add(await SendIcmpPingAsync(host, timeoutMs, cancellationToken));

            }

        }

        else

        {

            var client = httpClientFactory.CreateClient("PingClient");

            client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);



            for (var i = 0; i < packetCount; i++)

            {

                attempts.Add(await SendHttpPingAsync(client, endpoint, cancellationToken));

            }

        }



        return AggregateResults(endpoint.Id, attempts);

    }



    private static async Task<PingAttemptResult> SendIcmpPingAsync(

        string host,

        int timeoutMs,

        CancellationToken cancellationToken)

    {

        using var ping = new Ping();



        try

        {

            var reply = await ping.SendPingAsync(host, timeoutMs);

            cancellationToken.ThrowIfCancellationRequested();



            var isSuccess = reply.Status == IPStatus.Success;



            return new PingAttemptResult(

                isSuccess,

                isSuccess ? null : (int)reply.Status,

                isSuccess ? (int)reply.RoundtripTime : 0,

                isSuccess ? null : reply.Status.ToString());

        }

        catch (OperationCanceledException)

        {

            throw;

        }

        catch (PingException ex)

        {

            return new PingAttemptResult(false, null, 0, ex.Message);

        }

        catch (Exception ex)

        {

            return new PingAttemptResult(false, null, 0, ex.Message);

        }

    }



    private static async Task<PingAttemptResult> SendHttpPingAsync(

        HttpClient client,

        MonitoredEndpoint endpoint,

        CancellationToken cancellationToken)

    {

        var stopwatch = Stopwatch.StartNew();



        try

        {

            using var request = new HttpRequestMessage(

                new HttpMethod(endpoint.HttpMethod),

                EndpointAddress.ToRequestUrl(endpoint.Url));

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

            return new PingAttemptResult(false, null, 0, ex.Message);

        }

    }



    private static PingResultResponse AggregateResults(Guid endpointId, IReadOnlyList<PingAttemptResult> attempts)

    {

        var checkedAt = DateTime.UtcNow;

        var successCount = attempts.Count(a => a.IsSuccess);

        var allSuccess = successCount == attempts.Count;

        var packetsSent = attempts.Count;

        var packetsSucceeded = successCount;

        var successfulAttempts = attempts.Where(a => a.IsSuccess).ToList();



        var averageResponseTime = successfulAttempts.Count > 0

            ? (int)Math.Round(successfulAttempts.Average(a => a.ResponseTimeMs))

            : 0;



        var statusCode = attempts.LastOrDefault(a => a.StatusCode.HasValue)?.StatusCode;

        string? errorMessage = null;



        if (!allSuccess)

        {

            var firstFailure = attempts.FirstOrDefault(a => !a.IsSuccess);

            statusCode ??= firstFailure?.StatusCode;

            errorMessage = $"{packetsSucceeded}/{packetsSent} packets succeeded";

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

            checkedAt,

            packetsSent,

            packetsSucceeded);

    }



    private sealed record PingAttemptResult(

        bool IsSuccess,

        int? StatusCode,

        int ResponseTimeMs,

        string? ErrorMessage);

}


