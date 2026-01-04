using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;

namespace ExoAuth.Application.Features.Auth.Models;

/// <summary>
/// DTO for device information. Replaces DeviceSessionDto and TrustedDeviceDto.
/// </summary>
public sealed record DeviceDto(
    Guid Id,
    string DeviceId,
    string DisplayName,
    string? Name,
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
    DeviceStatus Status,
    bool IsCurrent,
    int? RiskScore,
    DateTime? TrustedAt,
    DateTime LastUsedAt,
    DateTime CreatedAt
)
{
    /// <summary>
    /// Maps a Device entity to a DeviceDto.
    /// </summary>
    /// <param name="device">The device entity.</param>
    /// <param name="currentDeviceId">The current device ID to determine IsCurrent.</param>
    /// <returns>The mapped DTO.</returns>
    public static DeviceDto FromEntity(Device device, Guid? currentDeviceId = null)
    {
        return new DeviceDto(
            Id: device.Id,
            DeviceId: device.DeviceId,
            DisplayName: device.DisplayName,
            Name: device.Name,
            Browser: device.Browser,
            BrowserVersion: device.BrowserVersion,
            OperatingSystem: device.OperatingSystem,
            OsVersion: device.OsVersion,
            DeviceType: device.DeviceType,
            IpAddress: device.IpAddress,
            Country: device.Country,
            CountryCode: device.CountryCode,
            City: device.City,
            LocationDisplay: device.LocationDisplay,
            Status: device.Status,
            IsCurrent: currentDeviceId.HasValue && device.Id == currentDeviceId.Value,
            RiskScore: device.RiskScore,
            TrustedAt: device.TrustedAt,
            LastUsedAt: device.LastUsedAt,
            CreatedAt: device.CreatedAt
        );
    }
}
