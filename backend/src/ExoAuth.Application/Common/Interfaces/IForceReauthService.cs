namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for managing force re-authentication flags in Redis.
/// Used to invalidate user sessions after permission changes, MFA reset, or password reset.
/// Flags are now session-based to ensure each session must re-authenticate individually.
/// </summary>
public interface IForceReauthService
{
    /// <summary>
    /// Sets the force re-auth flag for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID to flag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetFlagAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the force re-auth flag for all active sessions of a user.
    /// Used when permission changes, MFA is reset, or password is changed.
    /// </summary>
    /// <param name="userId">The user ID whose sessions should be flagged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of sessions that were flagged.</returns>
    Task<int> SetFlagForAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a session has the force re-auth flag set.
    /// </summary>
    /// <param name="sessionId">The session ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if flag is set, false otherwise.</returns>
    Task<bool> HasFlagAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the force re-auth flag for a session (after successful re-authentication).
    /// </summary>
    /// <param name="sessionId">The session ID to clear.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearFlagAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
