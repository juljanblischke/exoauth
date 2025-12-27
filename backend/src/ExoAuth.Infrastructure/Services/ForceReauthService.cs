using ExoAuth.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class ForceReauthService : IForceReauthService
{
    private readonly ICacheService _cache;
    private readonly ILogger<ForceReauthService> _logger;

    private const string KeyPrefix = "user:force-reauth:";
    private static readonly TimeSpan FlagTtl = TimeSpan.FromMinutes(15);

    public ForceReauthService(ICacheService cache, ILogger<ForceReauthService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task SetFlagAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{userId}";
        await _cache.SetAsync(key, new ForceReauthEntry(userId, DateTime.UtcNow), FlagTtl, cancellationToken);
        _logger.LogInformation("Force re-auth flag set for user {UserId}", userId);
    }

    public async Task<bool> HasFlagAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{userId}";
        return await _cache.ExistsAsync(key, cancellationToken);
    }

    public async Task ClearFlagAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{userId}";
        await _cache.RemoveAsync(key, cancellationToken);
        _logger.LogInformation("Force re-auth flag cleared for user {UserId}", userId);
    }

    private sealed record ForceReauthEntry(Guid UserId, DateTime SetAt);
}
