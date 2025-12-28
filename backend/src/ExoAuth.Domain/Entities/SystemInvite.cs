using System.Security.Cryptography;
using System.Text.Json;

namespace ExoAuth.Domain.Entities;

public sealed class SystemInvite : BaseEntity
{
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public JsonDocument PermissionIds { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime? ResentAt { get; private set; }
    public Guid InvitedBy { get; private set; }

    // Navigation properties
    public SystemUser InvitedByUser { get; private set; } = null!;

    private SystemInvite() { } // EF Core

    public static SystemInvite Create(
        string email,
        string firstName,
        string lastName,
        List<Guid> permissionIds,
        Guid invitedBy,
        string tokenHash,
        int expirationHours = 24)
    {
        return new SystemInvite
        {
            Email = email.ToLowerInvariant(),
            FirstName = firstName,
            LastName = lastName,
            TokenHash = tokenHash,
            PermissionIds = JsonDocument.Parse(JsonSerializer.Serialize(permissionIds)),
            ExpiresAt = DateTime.UtcNow.AddHours(expirationHours),
            InvitedBy = invitedBy
        };
    }

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

    public static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public bool IsAccepted => AcceptedAt.HasValue;

    public bool IsRevoked => RevokedAt.HasValue;

    public string Status
    {
        get
        {
            if (RevokedAt.HasValue) return "revoked";
            if (AcceptedAt.HasValue) return "accepted";
            if (DateTime.UtcNow > ExpiresAt) return "expired";
            return "pending";
        }
    }

    public void Accept()
    {
        AcceptedAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void MarkResent(string newTokenHash, int newExpirationHours = 24)
    {
        ResentAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddHours(newExpirationHours);
        TokenHash = newTokenHash;
        SetUpdated();
    }

    public List<Guid> GetPermissionIds()
    {
        return JsonSerializer.Deserialize<List<Guid>>(PermissionIds.RootElement.GetRawText()) ?? new List<Guid>();
    }
}
