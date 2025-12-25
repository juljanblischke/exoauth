using System.Text.Json;

namespace ExoAuth.Domain.Entities;

public sealed class SystemInvite : BaseEntity
{
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Token { get; private set; } = null!;
    public JsonDocument PermissionIds { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
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
        int expirationHours = 24)
    {
        return new SystemInvite
        {
            Email = email.ToLowerInvariant(),
            FirstName = firstName,
            LastName = lastName,
            Token = GenerateToken(),
            PermissionIds = JsonDocument.Parse(JsonSerializer.Serialize(permissionIds)),
            ExpiresAt = DateTime.UtcNow.AddHours(expirationHours),
            InvitedBy = invitedBy
        };
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public bool IsAccepted => AcceptedAt.HasValue;

    public void Accept()
    {
        AcceptedAt = DateTime.UtcNow;
        SetUpdated();
    }

    public List<Guid> GetPermissionIds()
    {
        return JsonSerializer.Deserialize<List<Guid>>(PermissionIds.RootElement.GetRawText()) ?? new List<Guid>();
    }

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
