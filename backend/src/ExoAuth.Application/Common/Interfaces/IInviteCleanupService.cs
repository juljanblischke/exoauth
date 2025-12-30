namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for cleaning up old expired/revoked invites.
/// </summary>
public interface IInviteCleanupService
{
    /// <summary>
    /// Deletes invites that have been expired or revoked for more than the specified retention period.
    /// </summary>
    /// <param name="retentionDays">Number of days to retain expired/revoked invites before deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of deleted invites.</returns>
    Task<int> CleanupExpiredAndRevokedInvitesAsync(int retentionDays, CancellationToken cancellationToken = default);
}
