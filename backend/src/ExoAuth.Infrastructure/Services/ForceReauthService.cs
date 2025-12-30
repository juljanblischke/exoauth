using ExoAuth.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class ForceReauthService : IForceReauthService
{
    private readonly ICacheService _cache;
    private readonly IDeviceSessionService _deviceSessionService;
    private readonly ILogger<ForceReauthService> _logger;

    private const string KeyPrefix = "session:force-reauth:";
    private static readonly TimeSpan FlagTtl = TimeSpan.FromMinutes(15);

    public ForceReauthService(
        ICacheService cache,
        IDeviceSessionService deviceSessionService,
        ILogger<ForceReauthService> logger)
    {
        _cache = cache;
        _deviceSessionService = deviceSessionService;
        _logger = logger;
    }

    public async Task SetFlagAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{sessionId}";
        await _cache.SetAsync(key, new ForceReauthEntry(sessionId, DateTime.UtcNow), FlagTtl, cancellationToken);
        _logger.LogInformation("Force re-auth flag set for session {SessionId}", sessionId);
    }

    public async Task<int> SetFlagForAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeSessions = await _deviceSessionService.GetActiveSessionsAsync(userId, cancellationToken);

        foreach (var session in activeSessions)
        {
            await SetFlagAsync(session.Id, cancellationToken);
        }

        _logger.LogInformation(
            "Force re-auth flag set for {SessionCount} sessions of user {UserId}",
            activeSessions.Count,
            userId);

        return activeSessions.Count;
    }

    public async Task<bool> HasFlagAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{sessionId}";
        return await _cache.ExistsAsync(key, cancellationToken);
    }

    public async Task ClearFlagAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{sessionId}";
        await _cache.RemoveAsync(key, cancellationToken);
        _logger.LogInformation("Force re-auth flag cleared for session {SessionId}", sessionId);
    }

    private sealed record ForceReauthEntry(Guid SessionId, DateTime SetAt);
}
