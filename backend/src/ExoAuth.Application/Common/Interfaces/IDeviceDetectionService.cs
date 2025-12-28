namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for parsing user agent strings to detect device information.
/// </summary>
public interface IDeviceDetectionService
{
    /// <summary>
    /// Parses a user agent string and returns device information.
    /// </summary>
    /// <param name="userAgent">The user agent string to parse.</param>
    /// <returns>Device information parsed from the user agent.</returns>
    DeviceInfo Parse(string? userAgent);
}

/// <summary>
/// Represents device information parsed from a user agent string.
/// </summary>
public sealed record DeviceInfo(
    string? Browser,
    string? BrowserVersion,
    string? OperatingSystem,
    string? OsVersion,
    string? DeviceType
)
{
    /// <summary>
    /// Gets a display name for the device.
    /// </summary>
    public string DisplayName
    {
        get
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(Browser))
                parts.Add(Browser);

            if (!string.IsNullOrEmpty(OperatingSystem))
                parts.Add($"on {OperatingSystem}");

            return parts.Count > 0 ? string.Join(" ", parts) : "Unknown Device";
        }
    }

    /// <summary>
    /// Returns an empty device info (all values null).
    /// </summary>
    public static DeviceInfo Empty => new(null, null, null, null, null);
}
