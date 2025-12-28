namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for tracking revoked sessions to immediately invalidate access tokens.
/// </summary>
public interface IRevokedSessionService
{
    /// <summary>
    /// Marks a session as revoked. Access tokens with this session ID will be rejected.
    /// </summary>
    /// <param name="sessionId">The session ID to revoke.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RevokeSessionAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Marks multiple sessions as revoked.
    /// </summary>
    /// <param name="sessionIds">The session IDs to revoke.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RevokeSessionsAsync(IEnumerable<Guid> sessionIds, CancellationToken ct = default);

    /// <summary>
    /// Checks if a session has been revoked.
    /// </summary>
    /// <param name="sessionId">The session ID to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the session is revoked.</returns>
    Task<bool> IsSessionRevokedAsync(Guid sessionId, CancellationToken ct = default);
}
