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

    private const string AttemptsKeyPrefix = "login:attempts:";
    private const string BlockedKeyPrefix = "login:blocked:";

    public BruteForceProtectionService(
        ICacheService cache,
        IConfiguration configuration,
        ILogger<BruteForceProtectionService> logger)
    {
        _cache = cache;
        _logger = logger;

        var bruteForceSection = configuration.GetSection("BruteForce");
        _maxAttempts = bruteForceSection.GetValue<int>("MaxAttempts", 5);
        var lockoutMinutes = bruteForceSection.GetValue<int>("LockoutMinutes", 15);
        _lockoutDuration = TimeSpan.FromMinutes(lockoutMinutes);
    }

    public async Task<bool> IsBlockedAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var blockedKey = $"{BlockedKeyPrefix}{normalizedEmail}";

        return await _cache.ExistsAsync(blockedKey, cancellationToken);
    }

    public async Task<(int Attempts, bool IsBlocked)> RecordFailedAttemptAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var attemptsKey = $"{AttemptsKeyPrefix}{normalizedEmail}";
        var blockedKey = $"{BlockedKeyPrefix}{normalizedEmail}";

        // Increment attempt counter
        var attempts = await _cache.IncrementAsync(attemptsKey, 1, _lockoutDuration, cancellationToken);

        _logger.LogDebug("Failed login attempt {Attempt}/{Max} for {Email}", attempts, _maxAttempts, email);

        // Check if should be blocked
        if (attempts >= _maxAttempts)
        {
            // Set blocked flag
            await _cache.SetAsync(blockedKey, new BlockedInfo(normalizedEmail, DateTime.UtcNow), _lockoutDuration, cancellationToken);
            _logger.LogWarning("Email {Email} blocked due to {Attempts} failed login attempts", email, attempts);
            return ((int)attempts, true);
        }

        return ((int)attempts, false);
    }

    public async Task ResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var attemptsKey = $"{AttemptsKeyPrefix}{normalizedEmail}";
        var blockedKey = $"{BlockedKeyPrefix}{normalizedEmail}";

        await _cache.RemoveAsync(attemptsKey, cancellationToken);
        await _cache.RemoveAsync(blockedKey, cancellationToken);

        _logger.LogDebug("Reset login attempts for {Email}", email);
    }

    public async Task<int> GetRemainingAttemptsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var attemptsKey = $"{AttemptsKeyPrefix}{normalizedEmail}";

        var attemptsInfo = await _cache.GetAsync<AttemptsInfo>(attemptsKey, cancellationToken);
        var currentAttempts = attemptsInfo?.Count ?? 0;

        return Math.Max(0, _maxAttempts - currentAttempts);
    }

    private sealed record BlockedInfo(string Email, DateTime BlockedAt);
    private sealed record AttemptsInfo(int Count);
}
