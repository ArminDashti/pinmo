using Pinmo.Core.Dtos;
using Pinmo.Core.Entities;

namespace Pinmo.Api;

internal static class EndpointMapper
{
    public static EndpointResponse ToResponse(this MonitoredEndpoint endpoint) =>
        new(
            endpoint.Id,
            endpoint.Url,
            endpoint.CreatedAt,
            endpoint.LastCheckedAt,
            endpoint.LastResponseTimeMs,
            endpoint.LastIsSuccess,
            endpoint.LastErrorMessage);

    public static string DeriveNameFromUrl(string url)
    {
        var normalized = url.Trim();

        if (normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["http://".Length..];
        }
        else if (normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["https://".Length..];
        }

        var slashIndex = normalized.IndexOf('/');
        var authority = slashIndex >= 0 ? normalized[..slashIndex] : normalized;
        var portSeparator = authority.IndexOf(':');

        return portSeparator > 0 ? authority[..portSeparator] : authority;
    }
}
