using ExoAuth.Domain.Entities;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for managing magic link tokens.
/// </summary>
public interface IMagicLinkService
{
    /// <summary>
    /// Creates a new magic link token for a user.
    /// Generates a URL token with collision prevention.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created token entity and the plain text token.</returns>
    Task<MagicLinkResult> CreateMagicLinkAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a magic link token.
    /// </summary>
    /// <param name="token">The plain text token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token entity if valid, null otherwise.</returns>
    Task<MagicLinkToken?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a magic link token as used.
    /// </summary>
    /// <param name="token">The token entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsUsedAsync(MagicLinkToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all pending magic link tokens for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateAllTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of creating a magic link token.
/// </summary>
public sealed record MagicLinkResult(
    MagicLinkToken Entity,
    string Token
);
