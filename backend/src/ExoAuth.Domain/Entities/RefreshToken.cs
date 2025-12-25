using System.Security.Cryptography;
using System.Text;
using ExoAuth.Domain.Enums;

namespace ExoAuth.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public UserType UserType { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? DeviceInfo { get; private set; }
    public string? IpAddress { get; private set; }

    private RefreshToken() { } // EF Core

    public static RefreshToken Create(
        Guid userId,
        UserType userType,
        string token,
        int expirationDays = 30,
        string? deviceInfo = null,
        string? ipAddress = null)
    {
        return new RefreshToken
        {
            UserId = userId,
            UserType = userType,
            TokenHash = HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            IsRevoked = false,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress
        };
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public bool IsValid => !IsRevoked && !IsExpired;

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        SetUpdated();
    }

    public bool ValidateToken(string token)
    {
        return TokenHash == HashToken(token);
    }

    public static string GenerateToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
