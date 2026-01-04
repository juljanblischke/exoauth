namespace ExoAuth.Domain.Entities;

/// <summary>
/// Represents a device session for tracking user logins across different devices.
/// Includes device information, location data, and trust status.
/// </summary>
public sealed class DeviceSession : BaseEntity
{
    public Guid UserId { get; private set; }
    public string DeviceId { get; private set; } = null!;
    public string? DeviceName { get; private set; }
    public string? DeviceFingerprint { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Browser { get; private set; }
    public string? BrowserVersion { get; private set; }
    public string? OperatingSystem { get; private set; }
    public string? OsVersion { get; private set; }
    public string? DeviceType { get; private set; }
    public string? IpAddress { get; private set; }
    public string? Country { get; private set; }
    public string? CountryCode { get; private set; }
    public string? City { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public Guid? TrustedDeviceId { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    // Navigation properties
    public SystemUser? User { get; set; }
    public TrustedDevice? TrustedDevice { get; set; }

    private DeviceSession() { } // EF Core

    /// <summary>
    /// Creates a new device session.
    /// </summary>
    public static DeviceSession Create(
        Guid userId,
        string deviceId,
        string? deviceName = null,
        string? deviceFingerprint = null,
        string? userAgent = null,
        string? ipAddress = null)
    {
        return new DeviceSession
        {
            UserId = userId,
            DeviceId = deviceId,
            DeviceName = deviceName,
            DeviceFingerprint = deviceFingerprint,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            IsRevoked = false,
            LastActivityAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Sets the device information (browser, OS, device type).
    /// </summary>
    public void SetDeviceInfo(
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
    /// Sets the location information from GeoIP lookup.
    /// </summary>
    public void SetLocation(
        string? country,
        string? countryCode,
        string? city,
        double? latitude,
        double? longitude)
    {
        Country = country;
        CountryCode = countryCode;
        City = city;
        Latitude = latitude;
        Longitude = longitude;
        SetUpdated();
    }

    /// <summary>
    /// Updates the IP address if it has changed.
    /// </summary>
    public void UpdateIpAddress(string? ipAddress)
    {
        if (IpAddress != ipAddress)
        {
            IpAddress = ipAddress;
            SetUpdated();
        }
    }

    /// <summary>
    /// Updates the last activity timestamp.
    /// </summary>
    public void RecordActivity()
    {
        LastActivityAt = DateTime.UtcNow;
        SetUpdated();
    }

    /// <summary>
    /// Links this session to a trusted device.
    /// </summary>
    public void LinkToTrustedDevice(Guid trustedDeviceId)
    {
        TrustedDeviceId = trustedDeviceId;
        SetUpdated();
    }

    /// <summary>
    /// Unlinks this session from a trusted device.
    /// </summary>
    public void UnlinkFromTrustedDevice()
    {
        TrustedDeviceId = null;
        SetUpdated();
    }

    /// <summary>
    /// Checks if this session is linked to a trusted device.
    /// </summary>
    public bool IsTrusted => TrustedDeviceId.HasValue;

    /// <summary>
    /// Sets a custom name for this device.
    /// </summary>
    public void SetName(string? name)
    {
        DeviceName = name;
        SetUpdated();
    }

    /// <summary>
    /// Revokes this session (invalidates it).
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        SetUpdated();
    }

    /// <summary>
    /// Checks if the session is active (not revoked).
    /// </summary>
    public bool IsActive => !IsRevoked;

    /// <summary>
    /// Gets a display name for the device, using custom name or generated from device info.
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(DeviceName))
                return DeviceName;

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
            if (string.IsNullOrEmpty(City) && string.IsNullOrEmpty(Country))
                return null;

            if (!string.IsNullOrEmpty(City) && !string.IsNullOrEmpty(Country))
                return $"{City}, {Country}";

            return Country ?? City;
        }
    }
}
