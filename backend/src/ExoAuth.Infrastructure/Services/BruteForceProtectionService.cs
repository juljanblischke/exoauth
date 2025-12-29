using ExoAuth.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class BruteForceProtectionService : IBruteForceProtectionService
{
    private readonly ICacheService _cache;
    private readonly ILogger<BruteForceProtectionService> _logger;
    private readonly int _maxAttempts;
    private readonly TimeSpan _lockoutDuration;
    private readonly int[] _progressiveDelays;
    private readonly int _notifyAfterSeconds;

    private const string AttemptsKeyPrefix = "login:attempts:";
    private const string BlockedKeyPrefix = "login:blocked:";
    private const string LockoutInfoKeyPrefix = "login:lockout:";

    public BruteForceProtectionService(
        ICacheService cache,
        IConfiguration configuration,
        ILogger<BruteForceProtectionService> logger)
    {
        _cache = cache;
        _logger = logger;

        var bruteForceSection = configuration.GetSection("BruteForce");
        _maxAttempts = bruteForceSection.GetValue<int>("MaxAttempts", 10);
        var lockoutMinutes = bruteForceSection.GetValue<int>("LockoutMinutes", 60);
        _lockoutDuration = TimeSpan.FromMinutes(lockoutMinutes);

        // Progressive lockout configuration
        var lockoutSection = configuration.GetSection("Lockout");
        _progressiveDelays = lockoutSection.GetSection("ProgressiveDelays").Get<int[]>()
            ?? [0, 0, 60, 120, 300, 600, 900, 1800, 3600];
        _notifyAfterSeconds = lockoutSection.GetValue<int>("NotifyAfterSeconds", 900);
    }

    public async Task<bool> IsBlockedAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var blockedKey = $"{BlockedKeyPrefix}{normalizedEmail}";

        return await _cache.ExistsAsync(blockedKey, cancellationToken);
    }

    public async Task<LockoutResult?> GetLockoutStatusAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var lockoutKey = $"{LockoutInfoKeyPrefix}{normalizedEmail}";

        var lockoutInfo = await _cache.GetAsync<LockoutInfo>(lockoutKey, cancellationToken);
        if (lockoutInfo is null)
            return null;

        // Check if lockout has expired
        if (lockoutInfo.LockedUntil <= DateTime.UtcNow)
            return null;

        return new LockoutResult(
            Attempts: lockoutInfo.Attempts,
            IsLocked: true,
            LockoutSeconds: lockoutInfo.LockoutSeconds,
            LockedUntil: lockoutInfo.LockedUntil,
            ShouldNotify: false // Already notified when locked
        );
    }

    public async Task<LockoutResult> RecordFailedAttemptAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var attemptsKey = $"{AttemptsKeyPrefix}{normalizedEmail}";
        var blockedKey = $"{BlockedKeyPrefix}{normalizedEmail}";
        var lockoutKey = $"{LockoutInfoKeyPrefix}{normalizedEmail}";

        // Increment attempt counter
        var attempts = await _cache.IncrementAsync(attemptsKey, 1, _lockoutDuration, cancellationToken);

        _logger.LogDebug("Failed login attempt {Attempt}/{Max} for {Email}", attempts, _maxAttempts, email);

        // Calculate progressive delay based on attempt count
        var lockoutSeconds = GetProgressiveDelay((int)attempts);

        // Check if we need to lock
        if (lockoutSeconds > 0)
        {
            var lockedUntil = DateTime.UtcNow.AddSeconds(lockoutSeconds);
            var lockoutTtl = TimeSpan.FromSeconds(lockoutSeconds + 60); // Add buffer to TTL

            var lockoutInfo = new LockoutInfo(
                normalizedEmail,
                (int)attempts,
                lockoutSeconds,
                lockedUntil,
                DateTime.UtcNow
            );

            // Set blocked flag (using string marker since cache requires reference type)
            await _cache.SetAsync(blockedKey, "blocked", TimeSpan.FromSeconds(lockoutSeconds), cancellationToken);

            // Store lockout info for status queries
            await _cache.SetAsync(lockoutKey, lockoutInfo, lockoutTtl, cancellationToken);

            var shouldNotify = lockoutSeconds >= _notifyAfterSeconds;

            _logger.LogWarning(
                "Email {Email} locked for {LockoutSeconds}s after {Attempts} failed attempts. Notify: {ShouldNotify}",
                email, lockoutSeconds, attempts, shouldNotify);

            return new LockoutResult(
                Attempts: (int)attempts,
                IsLocked: true,
                LockoutSeconds: lockoutSeconds,
                LockedUntil: lockedUntil,
                ShouldNotify: shouldNotify
            );
        }

        // Check if max attempts exceeded (permanent lockout until admin unlocks)
        if (attempts >= _maxAttempts)
        {
            var lockedUntil = DateTime.UtcNow.Add(_lockoutDuration);

            var lockoutInfo = new LockoutInfo(
                normalizedEmail,
                (int)attempts,
                (int)_lockoutDuration.TotalSeconds,
                lockedUntil,
                DateTime.UtcNow
            );

            await _cache.SetAsync(blockedKey, "blocked", _lockoutDuration, cancellationToken);
            await _cache.SetAsync(lockoutKey, lockoutInfo, _lockoutDuration, cancellationToken);

            _logger.LogWarning(
                "Email {Email} blocked for {LockoutDuration} due to {Attempts} failed login attempts",
                email, _lockoutDuration, attempts);

            return new LockoutResult(
                Attempts: (int)attempts,
                IsLocked: true,
                LockoutSeconds: (int)_lockoutDuration.TotalSeconds,
                LockedUntil: lockedUntil,
                ShouldNotify: true
            );
        }

        return new LockoutResult(
            Attempts: (int)attempts,
            IsLocked: false,
            LockoutSeconds: 0,
            LockedUntil: null,
            ShouldNotify: false
        );
    }

    public async Task ResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var attemptsKey = $"{AttemptsKeyPrefix}{normalizedEmail}";
        var blockedKey = $"{BlockedKeyPrefix}{normalizedEmail}";
        var lockoutKey = $"{LockoutInfoKeyPrefix}{normalizedEmail}";

        await _cache.RemoveAsync(attemptsKey, cancellationToken);
        await _cache.RemoveAsync(blockedKey, cancellationToken);
        await _cache.RemoveAsync(lockoutKey, cancellationToken);

        _logger.LogDebug("Reset login attempts for {Email}", email);
    }

    public async Task<int> GetRemainingAttemptsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var attemptsKey = $"{AttemptsKeyPrefix}{normalizedEmail}";

        var currentAttempts = await _cache.GetIntegerAsync(attemptsKey, cancellationToken) ?? 0;

        // Return attempts until first lockout delay (typically at attempt 3 based on default config)
        var firstLockoutAttempt = GetFirstLockoutAttempt();
        return Math.Max(0, firstLockoutAttempt - (int)currentAttempts);
    }

    /// <summary>
    /// Gets the progressive delay in seconds for the given attempt number.
    /// </summary>
    private int GetProgressiveDelay(int attemptNumber)
    {
        // attemptNumber is 1-based
        if (attemptNumber <= 0 || attemptNumber > _progressiveDelays.Length)
        {
            // Use the last delay for attempts beyond the configured range
            return attemptNumber > _progressiveDelays.Length
                ? _progressiveDelays[^1]
                : 0;
        }

        return _progressiveDelays[attemptNumber - 1];
    }

    /// <summary>
    /// Gets the first attempt number that triggers a lockout delay.
    /// </summary>
    private int GetFirstLockoutAttempt()
    {
        for (var i = 0; i < _progressiveDelays.Length; i++)
        {
            if (_progressiveDelays[i] > 0)
                return i + 1; // 1-based
        }
        return _progressiveDelays.Length + 1;
    }

    private sealed record LockoutInfo(
        string Email,
        int Attempts,
        int LockoutSeconds,
        DateTime LockedUntil,
        DateTime LockedAt
    );
}
