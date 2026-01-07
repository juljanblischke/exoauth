using ExoAuth.Application.Common.Models;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for rate limiting with sliding window algorithm and multiple time windows.
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Checks if a request is allowed based on the rate limit preset.
    /// Uses sliding window algorithm for accurate rate limiting.
    /// </summary>
    /// <param name="presetName">Name of the rate limit preset (e.g., "login", "default").</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="userId">User ID for per-user limiting (optional, for authenticated requests).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Rate limit result with allowed status and header information.</returns>
    Task<RateLimitResult> CheckRateLimitAsync(
        string presetName,
        string ipAddress,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a rate limit violation for auto-blacklist tracking.
    /// </summary>
    /// <param name="ipAddress">The IP address that violated the rate limit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the IP should be auto-blacklisted.</returns>
    Task<bool> RecordViolationAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the rate limit settings for a preset.
    /// </summary>
    /// <param name="presetName">Name of the preset.</param>
    /// <returns>The preset settings, or null if not found.</returns>
    RateLimitPreset? GetPreset(string presetName);
}
