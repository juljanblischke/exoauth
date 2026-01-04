namespace ExoAuth.Application.Features.Auth.Models;

/// <summary>
/// Data transfer object for trusted device information.
/// </summary>
public sealed record TrustedDeviceDto(
    Guid Id,
    string DeviceId,
    string Name,
    string? Browser,
    string? BrowserVersion,
    string? OperatingSystem,
    string? OsVersion,
    string? DeviceType,
    string? LastIpAddress,
    string? LastCountry,
    string? LastCity,
    string? LocationDisplay,
    bool IsCurrent,
    DateTime TrustedAt,
    DateTime? LastUsedAt
);
