namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for managing force re-authentication flags in Redis.
/// Used to invalidate user sessions after permission changes.
/// </summary>
public interface IForceReauthService
{
    /// <summary>
    /// Sets the force re-auth flag for a user.
    /// </summary>
    /// <param name="userId">The user ID to flag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetFlagAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has the force re-auth flag set.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if flag is set, false otherwise.</returns>
    Task<bool> HasFlagAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the force re-auth flag for a user (after successful login).
    /// </summary>
    /// <param name="userId">The user ID to clear.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearFlagAsync(Guid userId, CancellationToken cancellationToken = default);
}
