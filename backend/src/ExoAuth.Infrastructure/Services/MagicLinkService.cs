using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class MagicLinkService : IMagicLinkService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<MagicLinkService> _logger;
    private readonly int _expirationMinutes;
    private const int MaxRetries = 3;

    public MagicLinkService(
        IAppDbContext context,
        IConfiguration configuration,
        ILogger<MagicLinkService> logger)
    {
        _context = context;
        _logger = logger;
        _expirationMinutes = configuration.GetValue("Auth:MagicLinkExpiryMinutes", 15);
    }

    public async Task<MagicLinkResult> CreateMagicLinkAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Invalidate any existing tokens for this user first
        await InvalidateAllTokensAsync(userId, cancellationToken);

        // Generate with collision prevention
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var token = MagicLinkToken.GenerateToken();

            // Check for token hash collision (extremely unlikely but we check anyway)
            var tokenHash = HashForCheck(token);
            var exists = await _context.MagicLinkTokens
                .AnyAsync(x => x.TokenHash == tokenHash, cancellationToken);

            if (exists)
            {
                _logger.LogWarning("Token collision detected on attempt {Attempt}, regenerating", attempt + 1);
                continue;
            }

            var entity = MagicLinkToken.Create(userId, token, _expirationMinutes);

            await _context.MagicLinkTokens.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Magic link token created for user {UserId}", userId);

            return new MagicLinkResult(entity, token);
        }

        // This should never happen given the entropy of our tokens
        throw new InvalidOperationException("Failed to generate unique token after maximum retries");
    }

    public async Task<MagicLinkToken?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashForCheck(token);

        var magicLinkToken = await _context.MagicLinkTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (magicLinkToken is null)
        {
            _logger.LogDebug("Magic link token not found");
            return null;
        }

        if (!magicLinkToken.IsValid)
        {
            _logger.LogDebug("Magic link token is invalid (used: {IsUsed}, expired: {IsExpired})",
                magicLinkToken.IsUsed, magicLinkToken.IsExpired);
            return null;
        }

        return magicLinkToken;
    }

    public async Task MarkAsUsedAsync(MagicLinkToken token, CancellationToken cancellationToken = default)
    {
        token.MarkAsUsed();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Magic link token marked as used for user {UserId}", token.UserId);
    }

    public async Task InvalidateAllTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var pendingTokens = await _context.MagicLinkTokens
            .Where(x => x.UserId == userId && !x.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var token in pendingTokens)
        {
            token.MarkAsUsed();
        }

        if (pendingTokens.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Invalidated {Count} pending magic link tokens for user {UserId}",
                pendingTokens.Count, userId);
        }
    }

    /// <summary>
    /// Hash a token for lookup comparison.
    /// Must match the hashing in MagicLinkToken entity.
    /// </summary>
    private static string HashForCheck(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
