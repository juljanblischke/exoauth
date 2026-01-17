using ExoAuth.Domain.Entities;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for managing password reset tokens.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Creates a new password reset token for a user.
    /// Generates both a URL token and an 8-char code (XXXX-XXXX).
    /// Includes collision prevention for tokens.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created token entity and the plain text values (token, code).</returns>
    Task<PasswordResetResult> CreateResetTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a password reset token (URL token).
    /// </summary>
    /// <param name="token">The plain text token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token entity if valid, null otherwise.</returns>
    Task<PasswordResetToken?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a password reset code (XXXX-XXXX format).
    /// </summary>
    /// <param name="email">The user's email.</param>
    /// <param name="code">The plain text code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token entity if valid, null otherwise.</returns>
    Task<PasswordResetToken?> ValidateCodeAsync(string email, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a password reset token as used.
    /// </summary>
    /// <param name="token">The token entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsUsedAsync(PasswordResetToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all pending password reset tokens for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateAllTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of creating a password reset token.
/// </summary>
public sealed record PasswordResetResult(
    PasswordResetToken Entity,
    string Token,
    string Code
);
