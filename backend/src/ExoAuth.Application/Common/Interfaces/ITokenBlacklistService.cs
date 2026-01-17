namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for managing revoked token blacklist in Redis.
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Adds a token to the blacklist.
    /// </summary>
    /// <param name="tokenId">The token ID to blacklist.</param>
    /// <param name="expiresAt">When the token expires (for TTL calculation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BlacklistAsync(Guid tokenId, DateTime expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a token is blacklisted.
    /// </summary>
    /// <param name="tokenId">The token ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if blacklisted, false otherwise.</returns>
    Task<bool> IsBlacklistedAsync(Guid tokenId, CancellationToken cancellationToken = default);
}
