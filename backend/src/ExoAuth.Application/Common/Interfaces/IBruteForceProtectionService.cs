namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Result of a failed login attempt with progressive lockout information.
/// </summary>
public sealed record LockoutResult(
    int Attempts,
    bool IsLocked,
    int LockoutSeconds,
    DateTime? LockedUntil,
    bool ShouldNotify
);

/// <summary>
/// Service for brute force attack protection using Redis with progressive lockout.
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
    /// Gets the lockout status for an email, including when the lockout expires.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lockout information including when it expires, or null if not locked.</returns>
    Task<LockoutResult?> GetLockoutStatusAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed login attempt for an email with progressive lockout.
    /// </summary>
    /// <param name="email">The email that failed to login.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lockout result with attempt count, lockout duration, and notification flag.</returns>
    Task<LockoutResult> RecordFailedAttemptAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the failed attempt counter for an email after successful login.
    /// </summary>
    /// <param name="email">The email to reset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResetAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of remaining attempts before the email starts getting progressive lockouts.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of remaining attempts before lockout delays begin.</returns>
    Task<int> GetRemainingAttemptsAsync(string email, CancellationToken cancellationToken = default);
}
