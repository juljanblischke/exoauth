namespace ExoAuth.Application.Features.Auth.Models;

/// <summary>
/// Data transfer object for device session information.
/// </summary>
public sealed record DeviceSessionDto(
    Guid Id,
    string DeviceId,
    string DisplayName,
    string? DeviceName,
    string? Browser,
    string? BrowserVersion,
    string? OperatingSystem,
    string? OsVersion,
    string? DeviceType,
    string? IpAddress,
    string? Country,
    string? CountryCode,
    string? City,
    string? LocationDisplay,
    bool IsTrusted,
    bool IsCurrent,
    DateTime LastActivityAt,
    DateTime CreatedAt
);
