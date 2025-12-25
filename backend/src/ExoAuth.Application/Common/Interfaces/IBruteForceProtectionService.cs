namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for brute force attack protection using Redis.
/// </summary>
public interface IBruteForceProtectionService
{
    /// <summary>
    /// Checks if an email is currently blocked due to too many failed attempts.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if blocked, false otherwise.</returns>
    Task<bool> IsBlockedAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed login attempt for an email.
    /// </summary>
    /// <param name="email">The email that failed to login.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current number of failed attempts and whether the email is now blocked.</returns>
    Task<(int Attempts, bool IsBlocked)> RecordFailedAttemptAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the failed attempt counter for an email after successful login.
    /// </summary>
    /// <param name="email">The email to reset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResetAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of remaining attempts before the email is blocked.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of remaining attempts.</returns>
    Task<int> GetRemainingAttemptsAsync(string email, CancellationToken cancellationToken = default);
}
