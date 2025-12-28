using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class PasswordResetService : IPasswordResetService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<PasswordResetService> _logger;
    private readonly int _expirationMinutes;
    private const int MaxRetries = 3;

    public PasswordResetService(
        IAppDbContext context,
        IConfiguration configuration,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _logger = logger;
        _expirationMinutes = configuration.GetValue("Auth:PasswordResetExpiryMinutes", 15);
    }

    public async Task<PasswordResetResult> CreateResetTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Invalidate any existing tokens for this user first
        await InvalidateAllTokensAsync(userId, cancellationToken);

        // Generate with collision prevention
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var token = PasswordResetToken.GenerateToken();
            var code = PasswordResetToken.GenerateCode();

            // Check for token hash collision (extremely unlikely but we check anyway)
            var tokenHash = HashForCheck(token);
            var exists = await _context.PasswordResetTokens
                .AnyAsync(x => x.TokenHash == tokenHash, cancellationToken);

            if (exists)
            {
                _logger.LogWarning("Token collision detected on attempt {Attempt}, regenerating", attempt + 1);
                continue;
            }

            var entity = PasswordResetToken.Create(userId, token, code, _expirationMinutes);

            await _context.PasswordResetTokens.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password reset token created for user {UserId}", userId);

            return new PasswordResetResult(entity, token, code);
        }

        // This should never happen given the entropy of our tokens
        throw new InvalidOperationException("Failed to generate unique token after maximum retries");
    }

    public async Task<PasswordResetToken?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashForCheck(token);

        var resetToken = await _context.PasswordResetTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null)
        {
            _logger.LogDebug("Password reset token not found");
            return null;
        }

        if (!resetToken.IsValid)
        {
            _logger.LogDebug("Password reset token is invalid (used: {IsUsed}, expired: {IsExpired})",
                resetToken.IsUsed, resetToken.IsExpired);
            return null;
        }

        return resetToken;
    }

    public async Task<PasswordResetToken?> ValidateCodeAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();

        // Find the most recent valid token for this user
        var resetToken = await _context.PasswordResetTokens
            .Include(x => x.User)
            .Where(x => x.User != null && x.User.Email == normalizedEmail)
            .Where(x => !x.IsUsed && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (resetToken is null)
        {
            _logger.LogDebug("No valid password reset token found for email {Email}", normalizedEmail);
            return null;
        }

        if (!resetToken.ValidateCode(code))
        {
            _logger.LogDebug("Password reset code validation failed for email {Email}", normalizedEmail);
            return null;
        }

        return resetToken;
    }

    public async Task MarkAsUsedAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        token.MarkAsUsed();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset token marked as used for user {UserId}", token.UserId);
    }

    public async Task InvalidateAllTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var pendingTokens = await _context.PasswordResetTokens
            .Where(x => x.UserId == userId && !x.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var token in pendingTokens)
        {
            token.MarkAsUsed();
        }

        if (pendingTokens.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Invalidated {Count} pending password reset tokens for user {UserId}",
                pendingTokens.Count, userId);
        }
    }

    /// <summary>
    /// Hash a token for lookup comparison.
    /// Must match the hashing in PasswordResetToken entity.
    /// </summary>
    private static string HashForCheck(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
