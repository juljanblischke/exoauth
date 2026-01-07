using ExoAuth.Domain.Enums;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Result of an IP restriction check.
/// </summary>
public sealed record IpRestrictionCheckResult(
    bool IsBlacklisted,
    bool IsWhitelisted,
    string? Reason,
    DateTime? ExpiresAt
);

/// <summary>
/// Service for managing IP whitelist/blacklist restrictions.
/// </summary>
public interface IIpRestrictionService
{
    /// <summary>
    /// Checks if an IP address is restricted (whitelisted or blacklisted).
    /// Supports CIDR matching.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Check result with restriction status.</returns>
    Task<IpRestrictionCheckResult> CheckIpAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an IP to the blacklist automatically (e.g., from repeated rate limit violations).
    /// </summary>
    /// <param name="ipAddress">The IP address to blacklist.</param>
    /// <param name="reason">Reason for blacklisting.</param>
    /// <param name="durationMinutes">Duration of the blacklist in minutes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AutoBlacklistAsync(
        string ipAddress,
        string reason,
        int durationMinutes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cache for IP restrictions (called after CRUD operations).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateCacheAsync(CancellationToken cancellationToken = default);
}
