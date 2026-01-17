using ExoAuth.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class TokenBlacklistService : ITokenBlacklistService
{
    private readonly ICacheService _cache;
    private readonly ILogger<TokenBlacklistService> _logger;

    private const string KeyPrefix = "revoked:refresh:";

    public TokenBlacklistService(ICacheService cache, ILogger<TokenBlacklistService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task BlacklistAsync(Guid tokenId, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{tokenId}";

        // Calculate TTL - keep in blacklist until the token would have expired
        var ttl = expiresAt - DateTime.UtcNow;

        // Only blacklist if there's remaining TTL
        if (ttl > TimeSpan.Zero)
        {
            await _cache.SetAsync(key, new BlacklistEntry(tokenId, DateTime.UtcNow), ttl, cancellationToken);
            _logger.LogDebug("Token {TokenId} blacklisted until {ExpiresAt}", tokenId, expiresAt);
        }
        else
        {
            _logger.LogDebug("Token {TokenId} already expired, not adding to blacklist", tokenId);
        }
    }

    public async Task<bool> IsBlacklistedAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{tokenId}";
        return await _cache.ExistsAsync(key, cancellationToken);
    }

    private sealed record BlacklistEntry(Guid TokenId, DateTime BlacklistedAt);
}
