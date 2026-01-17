using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// Redis-based service for tracking revoked sessions.
/// Revoked session IDs are cached for the duration of access token lifetime.
/// </summary>
public sealed class RevokedSessionService : IRevokedSessionService
{
    private readonly RedisConnectionFactory _redisFactory;
    private readonly ILogger<RevokedSessionService> _logger;
    private readonly TimeSpan _ttl;

    private const string KeyPrefix = "revoked_session:";

    public RevokedSessionService(
        RedisConnectionFactory redisFactory,
        IConfiguration configuration,
        ILogger<RevokedSessionService> logger)
    {
        _redisFactory = redisFactory;
        _logger = logger;

        // Cache revoked sessions for access token lifetime + small buffer
        var accessTokenMinutes = configuration.GetValue("Jwt:AccessTokenExpirationMinutes", 15);
        _ttl = TimeSpan.FromMinutes(accessTokenMinutes + 1);
    }

    public async Task RevokeSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            var db = await _redisFactory.GetDatabaseAsync(ct);
            var key = $"{KeyPrefix}{sessionId}";

            await db.StringSetAsync(key, "1", _ttl);

            _logger.LogDebug("Marked session {SessionId} as revoked in cache", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark session {SessionId} as revoked", sessionId);
            // Don't throw - session will still be invalid via refresh token revocation
        }
    }

    public async Task RevokeSessionsAsync(IEnumerable<Guid> sessionIds, CancellationToken ct = default)
    {
        try
        {
            var db = await _redisFactory.GetDatabaseAsync(ct);
            var batch = db.CreateBatch();
            var tasks = new List<Task>();

            foreach (var sessionId in sessionIds)
            {
                var key = $"{KeyPrefix}{sessionId}";
                tasks.Add(batch.StringSetAsync(key, "1", _ttl));
            }

            batch.Execute();
            await Task.WhenAll(tasks);

            _logger.LogDebug("Marked {Count} sessions as revoked in cache", tasks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark sessions as revoked");
            // Don't throw - sessions will still be invalid via refresh token revocation
        }
    }

    public async Task<bool> IsSessionRevokedAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            var db = await _redisFactory.GetDatabaseAsync(ct);
            var key = $"{KeyPrefix}{sessionId}";

            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if session {SessionId} is revoked", sessionId);
            // On error, assume not revoked to avoid blocking legitimate requests
            return false;
        }
    }

    public async Task ClearRevokedSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            var db = await _redisFactory.GetDatabaseAsync(ct);
            var key = $"{KeyPrefix}{sessionId}";

            var deleted = await db.KeyDeleteAsync(key);

            if (deleted)
            {
                _logger.LogDebug("Cleared revoked session status for {SessionId}", sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear revoked session {SessionId}", sessionId);
            // Don't throw - best effort cleanup
        }
    }
}
