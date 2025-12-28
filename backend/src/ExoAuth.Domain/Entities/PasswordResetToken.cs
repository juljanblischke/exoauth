using System.Security.Cryptography;
using System.Text;

namespace ExoAuth.Domain.Entities;

/// <summary>
/// Represents a password reset token for a system user.
/// Supports both link-based (token) and code-based (XXXX-XXXX) reset.
/// </summary>
public sealed class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public string CodeHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    // Navigation property
    public SystemUser? User { get; set; }

    private PasswordResetToken() { } // EF Core

    /// <summary>
    /// Creates a new password reset token with both a URL token and an 8-char code (XXXX-XXXX).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="token">The generated URL token (will be hashed).</param>
    /// <param name="code">The generated XXXX-XXXX code (will be hashed).</param>
    /// <param name="expirationMinutes">Expiration time in minutes (default: 15).</param>
    public static PasswordResetToken Create(
        Guid userId,
        string token,
        string code,
        int expirationMinutes = 15)
    {
        return new PasswordResetToken
        {
            UserId = userId,
            TokenHash = HashValue(token),
            CodeHash = HashValue(NormalizeCode(code)),
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
    /// Validates the provided code against the stored hash.
    /// </summary>
    public bool ValidateCode(string code)
    {
        return CodeHash == HashValue(NormalizeCode(code));
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
    /// Generates a cryptographically secure URL token.
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

    /// <summary>
    /// Generates an 8-character alphanumeric code in XXXX-XXXX format.
    /// Uses uppercase letters and digits (no ambiguous chars: 0, O, I, L, 1).
    /// </summary>
    public static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // No 0, O, I, L, 1
        var code = new char[9]; // 8 chars + 1 dash

        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[8];
        rng.GetBytes(bytes);

        for (int i = 0; i < 8; i++)
        {
            var targetIndex = i < 4 ? i : i + 1; // Skip position 4 for dash
            code[targetIndex] = chars[bytes[i] % chars.Length];
        }

        code[4] = '-';
        return new string(code);
    }

    /// <summary>
    /// Normalizes a code by removing dashes and converting to uppercase.
    /// </summary>
    private static string NormalizeCode(string code)
    {
        return code.Replace("-", "").ToUpperInvariant();
    }

    private static string HashValue(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(bytes);
    }
}
