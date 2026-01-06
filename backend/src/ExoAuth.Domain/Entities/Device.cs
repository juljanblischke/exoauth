using System.Security.Cryptography;
using System.Text;
using ExoAuth.Domain.Enums;

namespace ExoAuth.Domain.Entities;

/// <summary>
/// Represents a unified device that combines device sessions, trust status, and approval requests.
/// Replaces DeviceSession, TrustedDevice, and DeviceApprovalRequest.
/// </summary>
public sealed class Device : BaseEntity
{
    public Guid UserId { get; private set; }
    public string DeviceId { get; private set; } = null!;
    public string? Fingerprint { get; private set; }
    public string? Name { get; private set; }

    // Device Info
    public string? UserAgent { get; private set; }
    public string? Browser { get; private set; }
    public string? BrowserVersion { get; private set; }
    public string? OperatingSystem { get; private set; }
    public string? OsVersion { get; private set; }
    public string? DeviceType { get; private set; }

    // Location
    public string? IpAddress { get; private set; }
    public string? Country { get; private set; }
    public string? CountryCode { get; private set; }
    public string? City { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    // Status
    public DeviceStatus Status { get; private set; }
    public DateTime? TrustedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime LastUsedAt { get; private set; }

    // Approval (temporary, cleared after approval)
    public string? ApprovalTokenHash { get; private set; }
    public string? ApprovalCodeHash { get; private set; }
    public DateTime? ApprovalExpiresAt { get; private set; }
    public int ApprovalAttempts { get; private set; }
    public int? RiskScore { get; private set; }
    public string? RiskFactors { get; private set; }

    // Navigation properties
    public SystemUser? User { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    private Device() { } // EF Core

    /// <summary>
    /// Creates a new device in PendingApproval status.
    /// </summary>
    public static Device CreatePending(
        Guid userId,
        string deviceId,
        string approvalToken,
        string approvalCode,
        int riskScore,
        string riskFactors,
        string? fingerprint = null,
        string? name = null,
        string? userAgent = null,
        string? ipAddress = null,
        int expirationMinutes = 30)
    {
        var now = DateTime.UtcNow;
        return new Device
        {
            UserId = userId,
            DeviceId = deviceId,
            Fingerprint = fingerprint,
            Name = name,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            Status = DeviceStatus.PendingApproval,
            LastUsedAt = now,
            ApprovalTokenHash = HashValue(approvalToken),
            ApprovalCodeHash = HashValue(NormalizeCode(approvalCode)),
            ApprovalExpiresAt = now.AddMinutes(expirationMinutes),
            ApprovalAttempts = 0,
            RiskScore = riskScore,
            RiskFactors = riskFactors
        };
    }

    /// <summary>
    /// Creates a new device that is already trusted (for known devices).
    /// </summary>
    public static Device CreateTrusted(
        Guid userId,
        string deviceId,
        string? fingerprint = null,
        string? name = null,
        string? userAgent = null,
        string? ipAddress = null)
    {
        var now = DateTime.UtcNow;
        return new Device
        {
            UserId = userId,
            DeviceId = deviceId,
            Fingerprint = fingerprint,
            Name = name,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            Status = DeviceStatus.Trusted,
            TrustedAt = now,
            LastUsedAt = now
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
    /// Records device usage (updates last used timestamp and optionally location).
    /// </summary>
    public void RecordUsage(string? ipAddress = null, string? country = null, string? city = null)
    {
        LastUsedAt = DateTime.UtcNow;
        if (ipAddress != null) IpAddress = ipAddress;
        if (country != null) Country = country;
        if (city != null) City = city;
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
        Fingerprint = fingerprint;
        SetUpdated();
    }

    /// <summary>
    /// Checks if the approval request is expired.
    /// </summary>
    public bool IsApprovalExpired => ApprovalExpiresAt.HasValue && DateTime.UtcNow > ApprovalExpiresAt.Value;

    /// <summary>
    /// Checks if the device is pending approval and the approval is still valid.
    /// </summary>
    public bool IsPendingAndValid => Status == DeviceStatus.PendingApproval && !IsApprovalExpired;

    /// <summary>
    /// Validates the provided approval token against the stored hash.
    /// </summary>
    public bool ValidateApprovalToken(string token)
    {
        return ApprovalTokenHash == HashValue(token);
    }

    /// <summary>
    /// Validates the provided approval code against the stored hash.
    /// </summary>
    public bool ValidateApprovalCode(string code)
    {
        return ApprovalCodeHash == HashValue(NormalizeCode(code));
    }

    /// <summary>
    /// Increments the failed approval code attempt counter.
    /// </summary>
    /// <returns>The new attempt count.</returns>
    public int IncrementApprovalAttempts()
    {
        ApprovalAttempts++;
        SetUpdated();
        return ApprovalAttempts;
    }

    /// <summary>
    /// Marks the device as trusted (approves the device).
    /// Clears approval-related fields.
    /// </summary>
    public void MarkTrusted()
    {
        Status = DeviceStatus.Trusted;
        TrustedAt = DateTime.UtcNow;
        ClearApprovalData();
        SetUpdated();
    }

    /// <summary>
    /// Marks the device as revoked.
    /// </summary>
    public void Revoke()
    {
        Status = DeviceStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        ClearApprovalData();
        SetUpdated();
    }

    /// <summary>
    /// Resets a device back to pending approval status (for re-verification scenarios like spoofing detection).
    /// </summary>
    public void ResetToPending(
        string approvalToken,
        string approvalCode,
        int riskScore,
        string riskFactors,
        int expirationMinutes = 30)
    {
        Status = DeviceStatus.PendingApproval;
        TrustedAt = null;
        RevokedAt = null;
        ApprovalTokenHash = HashValue(approvalToken);
        ApprovalCodeHash = HashValue(NormalizeCode(approvalCode));
        ApprovalExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        ApprovalAttempts = 0;
        RiskScore = riskScore;
        RiskFactors = riskFactors;
        SetUpdated();
    }

    /// <summary>
    /// Clears approval-related data after approval/denial.
    /// </summary>
    private void ClearApprovalData()
    {
        ApprovalTokenHash = null;
        ApprovalCodeHash = null;
        ApprovalExpiresAt = null;
        ApprovalAttempts = 0;
    }

    /// <summary>
    /// Checks if the device is trusted and not revoked.
    /// </summary>
    public bool IsTrusted => Status == DeviceStatus.Trusted;

    /// <summary>
    /// Checks if the device is revoked.
    /// </summary>
    public bool IsRevoked => Status == DeviceStatus.Revoked;

    /// <summary>
    /// Checks if the device is active (trusted and not revoked).
    /// </summary>
    public bool IsActive => Status == DeviceStatus.Trusted;

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
            if (string.IsNullOrEmpty(City) && string.IsNullOrEmpty(Country))
                return null;

            if (!string.IsNullOrEmpty(City) && !string.IsNullOrEmpty(Country))
                return $"{City}, {Country}";

            return Country ?? City;
        }
    }

    /// <summary>
    /// Generates a cryptographically secure URL token.
    /// </summary>
    public static string GenerateApprovalToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    /// <summary>
    /// Generates an 8-character alphanumeric code in XXXX-XXXX format.
    /// Uses uppercase letters and digits (no ambiguous chars: 0, O, I, L, 1).
    /// </summary>
    public static string GenerateApprovalCode()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // No 0, O, I, L, 1
        var code = new char[9]; // 8 chars + 1 dash

        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[8];
        rng.GetBytes(bytes);

        for (int i = 0; i < 8; i++)
        {
            var targetIndex = i < 4 ? i : i + 1; // Skip position 4 for dash
            code[targetIndex] = chars[bytes[i] % chars.Length];
        }

        code[4] = '-';
        return new string(code);
    }

    /// <summary>
    /// Normalizes a code by removing dashes and converting to uppercase.
    /// </summary>
    private static string NormalizeCode(string code)
    {
        return code.Replace("-", "").ToUpperInvariant();
    }

    private static string HashValue(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Helper method for checking token hash existence (used by service for collision detection).
    /// </summary>
    public static string HashForCheck(string token)
    {
        return HashValue(token);
    }
}
