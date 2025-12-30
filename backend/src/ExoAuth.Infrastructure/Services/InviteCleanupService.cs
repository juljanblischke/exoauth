using ExoAuth.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class InviteCleanupService : IInviteCleanupService
{
    private readonly IAppDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<InviteCleanupService> _logger;

    public InviteCleanupService(
        IAppDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<InviteCleanupService> logger)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<int> CleanupExpiredAndRevokedInvitesAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        var cutoffDate = _dateTimeProvider.UtcNow.AddDays(-retentionDays);

        _logger.LogInformation(
            "Starting invite cleanup. Deleting invites expired or revoked before {CutoffDate}",
            cutoffDate);

        // Find invites that are:
        // 1. Revoked more than retentionDays ago, OR
        // 2. Expired more than retentionDays ago (and not accepted)
        var invitesToDelete = await _dbContext.SystemInvites
            .Where(i =>
                (i.RevokedAt != null && i.RevokedAt < cutoffDate) ||
                (i.AcceptedAt == null && i.RevokedAt == null && i.ExpiresAt < cutoffDate))
            .ToListAsync(cancellationToken);

        if (invitesToDelete.Count == 0)
        {
            _logger.LogInformation("No invites found for cleanup");
            return 0;
        }

        _dbContext.SystemInvites.RemoveRange(invitesToDelete);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Invite cleanup completed. Deleted {Count} invites",
            invitesToDelete.Count);

        return invitesToDelete.Count;
    }
}
