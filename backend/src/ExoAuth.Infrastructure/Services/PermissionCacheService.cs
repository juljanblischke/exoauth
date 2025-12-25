using ExoAuth.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class PermissionCacheService : IPermissionCacheService
{
    private readonly ICacheService _cache;
    private readonly ILogger<PermissionCacheService> _logger;
    private readonly TimeSpan _cacheTtl;

    private const string KeyPrefix = "user:permissions:";

    public PermissionCacheService(
        ICacheService cache,
        IConfiguration configuration,
        ILogger<PermissionCacheService> logger)
    {
        _cache = cache;
        _logger = logger;

        var ttlMinutes = configuration.GetValue<int>("Cache:PermissionCacheTtlMinutes", 60);
        _cacheTtl = TimeSpan.FromMinutes(ttlMinutes);
    }

    public async Task<List<string>?> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{userId}";
        return await _cache.GetAsync<List<string>>(key, cancellationToken);
    }

    public async Task SetPermissionsAsync(Guid userId, List<string> permissions, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{userId}";
        await _cache.SetAsync(key, permissions, _cacheTtl, cancellationToken);
        _logger.LogDebug("Cached {Count} permissions for user {UserId}", permissions.Count, userId);
    }

    public async Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{userId}";
        await _cache.RemoveAsync(key, cancellationToken);
        _logger.LogDebug("Invalidated permission cache for user {UserId}", userId);
    }

    public async Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        await _cache.DeleteByPatternAsync($"{KeyPrefix}*", cancellationToken);
        _logger.LogInformation("Invalidated all permission caches");
    }

    public async Task<List<string>> GetOrSetPermissionsAsync(
        Guid userId,
        Func<Task<List<string>>> fetchFromDb,
        CancellationToken cancellationToken = default)
    {
        // Try cache first
        var cached = await GetPermissionsAsync(userId, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Permission cache hit for user {UserId}", userId);
            return cached;
        }

        // Cache miss - fetch from DB
        _logger.LogDebug("Permission cache miss for user {UserId}, fetching from DB", userId);
        var permissions = await fetchFromDb();

        // Cache the result
        await SetPermissionsAsync(userId, permissions, cancellationToken);

        return permissions;
    }
}
