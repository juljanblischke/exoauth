using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class SystemInviteService : ISystemInviteService
{
    private const int MaxRetries = 3;

    private readonly IAppDbContext _context;
    private readonly ILogger<SystemInviteService> _logger;

    public SystemInviteService(
        IAppDbContext context,
        ILogger<SystemInviteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InviteTokenResult> GenerateTokenAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var token = SystemInvite.GenerateToken();
            var tokenHash = SystemInvite.HashToken(token);

            // Check if token hash already exists (collision prevention)
            var exists = await _context.SystemInvites
                .AnyAsync(x => x.TokenHash == tokenHash, cancellationToken);

            if (exists)
            {
                _logger.LogWarning(
                    "System invite token collision detected on attempt {Attempt}/{MaxRetries}",
                    attempt + 1, MaxRetries);
                continue;
            }

            return new InviteTokenResult(token, tokenHash);
        }

        _logger.LogError("Failed to generate unique invite token after {MaxRetries} attempts", MaxRetries);
        throw new InvalidOperationException($"Failed to generate unique invite token after {MaxRetries} attempts");
    }

    public async Task<SystemInvite?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = SystemInvite.HashToken(token);

        return await _context.SystemInvites
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);
    }
}
