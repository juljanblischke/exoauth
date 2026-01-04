using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for managing trusted devices.
/// Trust is independent of sessions - revoking sessions doesn't remove trust.
/// </summary>
public interface ITrustedDeviceService
{
    /// <summary>
    /// Finds a trusted device by user ID and device ID (with optional fingerprint fallback).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="deviceFingerprint">Optional device fingerprint for additional matching.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The trusted device if found, null otherwise.</returns>
    Task<TrustedDevice?> FindAsync(
        Guid userId,
        string deviceId,
        string? deviceFingerprint = null,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a new trusted device for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="deviceInfo">Information about the device.</param>
    /// <param name="geoLocation">Geographic location information.</param>
    /// <param name="deviceFingerprint">Optional device fingerprint.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created trusted device.</returns>
    Task<TrustedDevice> AddAsync(
        Guid userId,
        string deviceId,
        DeviceInfo deviceInfo,
        GeoLocation geoLocation,
        string? deviceFingerprint = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all trusted devices for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of trusted devices.</returns>
    Task<List<TrustedDevice>> GetAllAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets a trusted device by its ID.
    /// </summary>
    /// <param name="deviceId">The trusted device ID (primary key).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The trusted device if found, null otherwise.</returns>
    Task<TrustedDevice?> GetByIdAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>
    /// Removes a trusted device.
    /// </summary>
    /// <param name="deviceId">The trusted device ID (primary key).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if removed, false if not found.</returns>
    Task<bool> RemoveAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>
    /// Removes all trusted devices for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of devices removed.</returns>
    Task<int> RemoveAllAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Renames a trusted device.
    /// </summary>
    /// <param name="deviceId">The trusted device ID (primary key).</param>
    /// <param name="name">The new name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if renamed, false if not found.</returns>
    Task<bool> RenameAsync(Guid deviceId, string? name, CancellationToken ct = default);

    /// <summary>
    /// Records usage of a trusted device (updates LastUsedAt, IP, location).
    /// </summary>
    /// <param name="deviceId">The trusted device ID (primary key).</param>
    /// <param name="ipAddress">The current IP address.</param>
    /// <param name="country">The current country.</param>
    /// <param name="city">The current city.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordUsageAsync(
        Guid deviceId,
        string? ipAddress,
        string? country,
        string? city,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a user has any trusted devices.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user has at least one trusted device.</returns>
    Task<bool> HasAnyAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Finds a trusted device that matches the given device session.
    /// </summary>
    /// <param name="session">The device session to match against.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching trusted device if found, null otherwise.</returns>
    Task<TrustedDevice?> FindBySessionAsync(DeviceSession session, CancellationToken ct = default);
}
