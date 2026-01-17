using ExoAuth.Application.Common.Interfaces;
using UAParser;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// Service for parsing user agent strings using UAParser.
/// </summary>
public sealed class DeviceDetectionService : IDeviceDetectionService
{
    private readonly Parser _parser;

    public DeviceDetectionService()
    {
        _parser = Parser.GetDefault();
    }

    public DeviceInfo Parse(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return DeviceInfo.Empty;

        try
        {
            var clientInfo = _parser.Parse(userAgent);

            var browser = clientInfo.UA?.Family;
            var browserVersion = GetVersionString(clientInfo.UA?.Major, clientInfo.UA?.Minor, clientInfo.UA?.Patch);

            var os = clientInfo.OS?.Family;
            var osVersion = GetVersionString(clientInfo.OS?.Major, clientInfo.OS?.Minor, clientInfo.OS?.Patch);

            var deviceType = DetermineDeviceType(clientInfo.Device?.Family, userAgent);

            return new DeviceInfo(
                Browser: browser,
                BrowserVersion: browserVersion,
                OperatingSystem: os,
                OsVersion: osVersion,
                DeviceType: deviceType
            );
        }
        catch
        {
            return DeviceInfo.Empty;
        }
    }

    private static string? GetVersionString(string? major, string? minor, string? patch)
    {
        if (string.IsNullOrEmpty(major))
            return null;

        var version = major;

        if (!string.IsNullOrEmpty(minor))
        {
            version += $".{minor}";

            if (!string.IsNullOrEmpty(patch))
            {
                version += $".{patch}";
            }
        }

        return version;
    }

    private static string DetermineDeviceType(string? deviceFamily, string userAgent)
    {
        var ua = userAgent.ToLowerInvariant();

        // Check for specific device types
        if (ua.Contains("mobile") || ua.Contains("android") && !ua.Contains("tablet"))
            return "Mobile";

        if (ua.Contains("tablet") || ua.Contains("ipad"))
            return "Tablet";

        if (ua.Contains("smart-tv") || ua.Contains("smarttv") || ua.Contains("tv"))
            return "TV";

        if (ua.Contains("bot") || ua.Contains("crawler") || ua.Contains("spider"))
            return "Bot";

        // Check device family
        if (!string.IsNullOrEmpty(deviceFamily))
        {
            var family = deviceFamily.ToLowerInvariant();

            if (family.Contains("iphone") || family.Contains("android"))
                return "Mobile";

            if (family.Contains("ipad"))
                return "Tablet";
        }

        // Default to desktop for web browsers
        if (ua.Contains("windows") || ua.Contains("macintosh") || ua.Contains("linux"))
            return "Desktop";

        return "Unknown";
    }
}
