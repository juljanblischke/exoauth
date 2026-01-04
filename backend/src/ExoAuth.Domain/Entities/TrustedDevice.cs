namespace ExoAuth.Domain.Entities;

/// <summary>
/// Represents a trusted device for a user.
/// Trust is independent of sessions - revoking sessions doesn't remove trust.
/// </summary>
public sealed class TrustedDevice : BaseEntity
{
    public Guid UserId { get; private set; }
    public string DeviceId { get; private set; } = null!;
    public string? DeviceFingerprint { get; private set; }
    public string? Name { get; private set; }
    public string? Browser { get; private set; }
    public string? BrowserVersion { get; private set; }
    public string? OperatingSystem { get; private set; }
    public string? OsVersion { get; private set; }
    public string? DeviceType { get; private set; }
    public DateTime TrustedAt { get; private set; }
    public DateTime LastUsedAt { get; private set; }
    public string? LastIpAddress { get; private set; }
    public string? LastCountry { get; private set; }
    public string? LastCity { get; private set; }

    // Navigation property
    public SystemUser? User { get; set; }

    private TrustedDevice() { } // EF Core

    /// <summary>
    /// Creates a new trusted device entry.
    /// </summary>
    public static TrustedDevice Create(
        Guid userId,
        string deviceId,
        string? deviceFingerprint = null,
        string? name = null,
        string? browser = null,
        string? browserVersion = null,
        string? operatingSystem = null,
        string? osVersion = null,
        string? deviceType = null,
        string? ipAddress = null,
        string? country = null,
        string? city = null)
    {
        var now = DateTime.UtcNow;
        return new TrustedDevice
        {
            UserId = userId,
            DeviceId = deviceId,
            DeviceFingerprint = deviceFingerprint,
            Name = name,
            Browser = browser,
            BrowserVersion = browserVersion,
            OperatingSystem = operatingSystem,
            OsVersion = osVersion,
            DeviceType = deviceType,
            TrustedAt = now,
            LastUsedAt = now,
            LastIpAddress = ipAddress,
            LastCountry = country,
            LastCity = city
        };
    }

    /// <summary>
    /// Updates the last used information when the device is used for a login.
    /// </summary>
    public void RecordUsage(string? ipAddress = null, string? country = null, string? city = null)
    {
        LastUsedAt = DateTime.UtcNow;
        LastIpAddress = ipAddress;
        LastCountry = country;
        LastCity = city;
        SetUpdated();
    }

    /// <summary>
    /// Sets a custom name for this device.
    /// </summary>
    public void SetName(string? name)
    {
        Name = name;
        SetUpdated();
    }

    /// <summary>
    /// Updates device fingerprint (e.g., after browser update).
    /// </summary>
    public void UpdateFingerprint(string? fingerprint)
    {
        DeviceFingerprint = fingerprint;
        SetUpdated();
    }

    /// <summary>
    /// Updates device info (browser, OS, etc.).
    /// </summary>
    public void UpdateDeviceInfo(
        string? browser,
        string? browserVersion,
        string? operatingSystem,
        string? osVersion,
        string? deviceType)
    {
        Browser = browser;
        BrowserVersion = browserVersion;
        OperatingSystem = operatingSystem;
        OsVersion = osVersion;
        DeviceType = deviceType;
        SetUpdated();
    }

    /// <summary>
    /// Gets a display name for the device, using custom name or generated from device info.
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(Name))
                return Name;

            var parts = new List<string>();

            if (!string.IsNullOrEmpty(Browser))
                parts.Add(Browser);

            if (!string.IsNullOrEmpty(OperatingSystem))
                parts.Add($"on {OperatingSystem}");

            return parts.Count > 0 ? string.Join(" ", parts) : "Unknown Device";
        }
    }

    /// <summary>
    /// Gets a location display string.
    /// </summary>
    public string? LocationDisplay
    {
        get
        {
            if (string.IsNullOrEmpty(LastCity) && string.IsNullOrEmpty(LastCountry))
                return null;

            if (!string.IsNullOrEmpty(LastCity) && !string.IsNullOrEmpty(LastCountry))
                return $"{LastCity}, {LastCountry}";

            return LastCountry ?? LastCity;
        }
    }
}
