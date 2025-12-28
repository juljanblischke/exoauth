using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for managing device sessions.
/// </summary>
public interface IDeviceSessionService
{
    /// <summary>
    /// Creates or updates a device session for a user login.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="deviceId">The device ID (generated client-side or server-side).</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <param name="ipAddress">The IP address.</param>
    /// <param name="deviceFingerprint">Optional device fingerprint.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the device session and flags indicating if it's a new device/location.</returns>
    Task<(DeviceSession Session, bool IsNewDevice, bool IsNewLocation)> CreateOrUpdateSessionAsync(
        Guid userId,
        string deviceId,
        string? userAgent,
        string? ipAddress,
        string? deviceFingerprint = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all active sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of active device sessions.</returns>
    Task<List<DeviceSession>> GetActiveSessionsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The device session, or null if not found.</returns>
    Task<DeviceSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Records activity on a session (updates LastActivityAt).
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="ipAddress">The current IP address.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordActivityAsync(Guid sessionId, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Revokes a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the session was revoked, false if not found.</returns>
    Task<bool> RevokeSessionAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Revokes all sessions for a user except the specified one.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="exceptSessionId">The session ID to keep active (typically the current session).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of sessions revoked.</returns>
    Task<int> RevokeAllSessionsExceptAsync(Guid userId, Guid? exceptSessionId, CancellationToken ct = default);

    /// <summary>
    /// Updates the trust status of a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="trusted">Whether to trust the session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the session was updated, false if not found.</returns>
    Task<bool> SetTrustStatusAsync(Guid sessionId, bool trusted, CancellationToken ct = default);

    /// <summary>
    /// Sets a custom name for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="name">The custom name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the session was updated, false if not found.</returns>
    Task<bool> SetSessionNameAsync(Guid sessionId, string? name, CancellationToken ct = default);

    /// <summary>
    /// Generates a unique device ID for server-side device identification.
    /// </summary>
    /// <returns>A new device ID.</returns>
    string GenerateDeviceId();
}
