using System.Net.NetworkInformation;

namespace Pinmo.Core;

public static class PingTimeoutHelper
{
    public static bool IsTimeout(int? statusCode, string? errorMessage)
    {
        if (statusCode == (int)IPStatus.TimedOut)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return false;
        }

        return errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("timed out", StringComparison.OrdinalIgnoreCase);
    }
}
