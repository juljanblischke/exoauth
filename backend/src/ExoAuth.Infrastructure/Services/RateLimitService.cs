using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Infrastructure.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// Rate limiting service using sliding window algorithm with multiple time windows.
/// </summary>
public sealed class RateLimitService : IRateLimitService
{
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly RateLimitSettings _settings;
    private readonly ILogger<RateLimitService> _logger;

    private const string RateLimitKeyPrefix = "ratelimit:";
    private const string ViolationKeyPrefix = "ratelimit:violation:";

    public RateLimitService(
        IRedisConnectionFactory redisFactory,
        IOptions<RateLimitSettings> settings,
        ILogger<RateLimitService> logger)
    {
        _redisFactory = redisFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public RateLimitPreset? GetPreset(string presetName)
    {
        return _settings.Presets.TryGetValue(presetName, out var preset) ? preset : null;
    }

    /// <inheritdoc />
    public async Task<RateLimitResult> CheckRateLimitAsync(
        string presetName,
        string ipAddress,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return RateLimitResult.Allowed(int.MaxValue, int.MaxValue, 0);
        }

        if (!_settings.Presets.TryGetValue(presetName, out var preset))
        {
            _logger.LogWarning("Rate limit preset '{PresetName}' not found, using 'default'", presetName);
            preset = _settings.Presets.GetValueOrDefault("default", new RateLimitPreset { PerMinute = 100, PerHour = 1000 });
        }

        var db = await _redisFactory.GetDatabaseAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var nowMs = now.ToUnixTimeMilliseconds();

        // Build key identifier: prefer userId for authenticated requests, fallback to IP
        var identifier = userId.HasValue ? $"user:{userId.Value}" : $"ip:{ipAddress}";

        // Check minute window first (shorter window is typically more restrictive per-second)
        var minuteResult = await CheckWindowAsync(
            db, presetName, identifier, preset.PerMinute, TimeSpan.FromMinutes(1), nowMs, "minute");

        if (!minuteResult.IsAllowed)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {Identifier} on preset '{PresetName}' (minute window): {Remaining}/{Limit}",
                identifier, presetName, minuteResult.Remaining, minuteResult.Limit);
            return minuteResult;
        }

        // Check hour window
        var hourResult = await CheckWindowAsync(
            db, presetName, identifier, preset.PerHour, TimeSpan.FromHours(1), nowMs, "hour");

        if (!hourResult.IsAllowed)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {Identifier} on preset '{PresetName}' (hour window): {Remaining}/{Limit}",
                identifier, presetName, hourResult.Remaining, hourResult.Limit);
            return hourResult;
        }

        // Return the minute result (shows remaining for the smaller window which is more relevant)
        return minuteResult;
    }

    /// <inheritdoc />
    public async Task<bool> RecordViolationAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (!_settings.AutoBlacklist.Enabled)
        {
            return false;
        }

        var db = await _redisFactory.GetDatabaseAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var nowMs = now.ToUnixTimeMilliseconds();
        var windowMs = _settings.AutoBlacklist.WithinMinutes * 60 * 1000;
        var windowStart = nowMs - windowMs;

        var key = $"{ViolationKeyPrefix}{ipAddress}";

        // Use sorted set for sliding window of violations
        var transaction = db.CreateTransaction();

        // Remove old violations outside the window
        _ = transaction.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart, Exclude.Stop);

        // Add current violation
        _ = transaction.SortedSetAddAsync(key, Guid.NewGuid().ToString(), nowMs);

        // Set expiration
        _ = transaction.KeyExpireAsync(key, TimeSpan.FromMinutes(_settings.AutoBlacklist.WithinMinutes + 1));

        await transaction.ExecuteAsync();

        // Count violations
        var violationCount = await db.SortedSetLengthAsync(key);

        var shouldBlacklist = violationCount >= _settings.AutoBlacklist.ViolationThreshold;

        if (shouldBlacklist)
        {
            _logger.LogWarning(
                "IP {IpAddress} exceeded violation threshold ({Count}/{Threshold}) - triggering auto-blacklist",
                ipAddress, violationCount, _settings.AutoBlacklist.ViolationThreshold);

            // Clean up the violation tracking after triggering blacklist
            await db.KeyDeleteAsync(key);
        }

        return shouldBlacklist;
    }

    /// <summary>
    /// Checks rate limit for a specific time window using sliding window algorithm.
    /// </summary>
    private async Task<RateLimitResult> CheckWindowAsync(
        IDatabase db,
        string presetName,
        string identifier,
        int limit,
        TimeSpan window,
        long nowMs,
        string windowName)
    {
        var key = $"{RateLimitKeyPrefix}{presetName}:{windowName}:{identifier}";
        var windowMs = (long)window.TotalMilliseconds;
        var windowStart = nowMs - windowMs;

        // Lua script for atomic sliding window operation
        // This is more efficient than multiple round trips
        var script = @"
            -- Remove old entries outside the window
            redis.call('ZREMRANGEBYSCORE', KEYS[1], 0, ARGV[1])
            
            -- Count current entries
            local count = redis.call('ZCARD', KEYS[1])
            
            -- If under limit, add new entry
            if count < tonumber(ARGV[2]) then
                redis.call('ZADD', KEYS[1], ARGV[3], ARGV[4])
                redis.call('PEXPIRE', KEYS[1], ARGV[5])
                return {1, tonumber(ARGV[2]) - count - 1}
            end
            
            -- Get the oldest entry timestamp for reset calculation
            local oldest = redis.call('ZRANGE', KEYS[1], 0, 0, 'WITHSCORES')
            local resetAt = 0
            if oldest[2] then
                resetAt = tonumber(oldest[2]) + tonumber(ARGV[6])
            end
            
            return {0, 0, resetAt}
        ";

        var requestId = Guid.NewGuid().ToString();
        var expireMs = windowMs + 10000; // Add 10s buffer to TTL

        var result = await db.ScriptEvaluateAsync(
            script,
            new RedisKey[] { key },
            new RedisValue[]
            {
                windowStart,           // ARGV[1] - window start timestamp
                limit,                 // ARGV[2] - limit
                nowMs,                 // ARGV[3] - current timestamp (score)
                requestId,             // ARGV[4] - unique request id (member)
                expireMs,              // ARGV[5] - TTL in ms
                windowMs               // ARGV[6] - window size in ms
            });

        var resultArray = (RedisResult[])result!;
        var allowed = (int)resultArray[0] == 1;
        var remaining = (int)resultArray[1];

        if (allowed)
        {
            // Calculate reset time (when the oldest entry in window expires)
            var resetAt = (nowMs + windowMs) / 1000; // Convert to Unix seconds
            return RateLimitResult.Allowed(limit, remaining, resetAt);
        }

        // Rate limit exceeded
        var resetAtMs = resultArray.Length > 2 ? (long)resultArray[2] : nowMs + windowMs;
        var resetAtSeconds = resetAtMs / 1000;
        var retryAfterSeconds = Math.Max(1, (int)((resetAtMs - nowMs) / 1000));

        return RateLimitResult.Exceeded(limit, 0, resetAtSeconds, retryAfterSeconds, windowName);
    }
}
