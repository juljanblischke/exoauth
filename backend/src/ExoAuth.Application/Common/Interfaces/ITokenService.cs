using ExoAuth.Domain.Enums;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an access token for a user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="email">The user's email.</param>
    /// <param name="userType">The type of user.</param>
    /// <param name="permissions">The user's permissions.</param>
    /// <param name="sessionId">Optional session ID to include in the token.</param>
    /// <returns>The generated JWT access token.</returns>
    string GenerateAccessToken(Guid userId, string email, UserType userType, IEnumerable<string> permissions, Guid? sessionId = null);

    /// <summary>
    /// Generates a refresh token.
    /// </summary>
    /// <returns>The generated refresh token string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates an access token and returns the claims.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>The token claims if valid, null otherwise.</returns>
    TokenClaims? ValidateAccessToken(string token);

    /// <summary>
    /// Gets the expiration time for access tokens.
    /// </summary>
    TimeSpan AccessTokenExpiration { get; }

    /// <summary>
    /// Gets the expiration time for refresh tokens.
    /// </summary>
    TimeSpan RefreshTokenExpiration { get; }

    /// <summary>
    /// Gets the expiration time in days for refresh tokens when "Remember Me" is enabled.
    /// </summary>
    int RememberMeExpirationDays { get; }
}

/// <summary>
/// Claims extracted from a JWT token.
/// </summary>
public sealed record TokenClaims(
    Guid UserId,
    string Email,
    UserType UserType,
    IReadOnlyList<string> Permissions,
    DateTime ExpiresAt
);
