using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly RedisConnectionFactory _connectionFactory;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheService(RedisConnectionFactory connectionFactory, ILogger<RedisCacheService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var db = await _connectionFactory.GetDatabaseAsync(ct);
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<T>(value!, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get cache key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var db = await _connectionFactory.GetDatabaseAsync(ct);
            var json = JsonSerializer.Serialize(value, JsonOptions);

            if (expiration.HasValue)
            {
                await db.StringSetAsync(key, json, expiry: expiration.Value);
            }
            else
            {
                await db.StringSetAsync(key, json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = await _connectionFactory.GetDatabaseAsync(ct);
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = await _connectionFactory.GetDatabaseAsync(ct);
            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check cache key existence: {Key}", key);
            return false;
        }
    }

    public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        try
        {
            var db = await _connectionFactory.GetDatabaseAsync(ct);
            var result = await db.StringIncrementAsync(key, value);

            if (expiration.HasValue)
            {
                await db.KeyExpireAsync(key, expiration);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to increment cache key: {Key}", key);
            return 0;
        }
    }

    public async Task<long?> GetIntegerAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = await _connectionFactory.GetDatabaseAsync(ct);
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
                return null;

            if (long.TryParse(value, out var result))
                return result;

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get integer cache key: {Key}", key);
            return null;
        }
    }

    public async Task DeleteByPatternAsync(string pattern, CancellationToken ct = default)
    {
        try
        {
            var connection = await _connectionFactory.GetConnectionAsync(ct);
            var db = connection.GetDatabase();

            foreach (var endpoint in connection.GetEndPoints())
            {
                var server = connection.GetServer(endpoint);
                var keys = server.Keys(pattern: pattern).ToArray();

                if (keys.Length > 0)
                {
                    await db.KeyDeleteAsync(keys);
                    _logger.LogInformation("Deleted {Count} keys matching pattern: {Pattern}", keys.Length, pattern);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete keys by pattern: {Pattern}", pattern);
        }
    }
}
