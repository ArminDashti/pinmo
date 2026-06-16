using System.Net;

namespace Pinmo.Core;

public static class EndpointAddress
{
    public static bool TryNormalize(string? input, out string normalized, out string? errorMessage)
    {
        normalized = string.Empty;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "URL or IP address is required.";
            return false;
        }

        var value = input.Trim();

        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            value = value["http://".Length..];
        }
        else if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            value = value["https://".Length..];
        }

        value = value.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "URL or IP address is required.";
            return false;
        }

        if (!IsValidHostAddress(value))
        {
            errorMessage = "Enter a hostname, path, or IP address without http:// or https:// (e.g. example.com or 192.168.1.1).";
            return false;
        }

        normalized = value;
        return true;
    }

    public static string ToRequestUrl(string storedAddress)
    {
        if (storedAddress.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || storedAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return storedAddress;
        }

        return $"http://{storedAddress}";
    }

    public static bool ShouldUseIcmpPing(string storedAddress) =>
        storedAddress.IndexOf('/') < 0;

    public static string GetPingHost(string storedAddress)
    {
        var authority = storedAddress;
        var portSeparator = authority.LastIndexOf(':');

        if (portSeparator > 0 && authority.IndexOf(':') == portSeparator)
        {
            return authority[..portSeparator];
        }

        return authority;
    }

    private static bool IsValidHostAddress(string value)
    {
        var slashIndex = value.IndexOf('/');
        var authority = slashIndex >= 0 ? value[..slashIndex] : value;

        if (string.IsNullOrWhiteSpace(authority))
        {
            return false;
        }

        var host = authority;
        var portSeparator = authority.LastIndexOf(':');

        if (portSeparator > 0 && authority.IndexOf(':') == portSeparator)
        {
            host = authority[..portSeparator];

            if (!int.TryParse(authority[(portSeparator + 1)..], out var port) || port is < 1 or > 65535)
            {
                return false;
            }
        }

        if (IPAddress.TryParse(host, out _))
        {
            return true;
        }

        return Uri.CheckHostName(host) != UriHostNameType.Unknown;
    }
}
