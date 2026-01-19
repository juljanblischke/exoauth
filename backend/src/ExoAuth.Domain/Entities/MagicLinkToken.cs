using System.Security.Cryptography;
using System.Text;

namespace ExoAuth.Domain.Entities;

/// <summary>
/// Represents a magic link authentication token for a system user.
/// Provides passwordless authentication via time-limited, single-use tokens.
/// </summary>
public sealed class MagicLinkToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    // Navigation property
    public SystemUser? User { get; set; }

    private MagicLinkToken() { } // EF Core

    /// <summary>
    /// Creates a new magic link token for passwordless authentication.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="token">The generated token (will be hashed).</param>
    /// <param name="expirationMinutes">Expiration time in minutes (default: 15).</param>
    public static MagicLinkToken Create(
        Guid userId,
        string token,
        int expirationMinutes = 15)
    {
        return new MagicLinkToken
        {
            UserId = userId,
            TokenHash = HashValue(token),
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            IsUsed = false
        };
    }

    /// <summary>
    /// Checks if the token is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Checks if the token is still valid (not used and not expired).
    /// </summary>
    public bool IsValid => !IsUsed && !IsExpired;

    /// <summary>
    /// Validates the provided token against the stored hash.
    /// </summary>
    public bool ValidateToken(string token)
    {
        return TokenHash == HashValue(token);
    }

    /// <summary>
    /// Marks the token as used.
    /// </summary>
    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        SetUpdated();
    }

    /// <summary>
    /// Generates a cryptographically secure token.
    /// </summary>
    public static string GenerateToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string HashValue(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(bytes);
    }
}
