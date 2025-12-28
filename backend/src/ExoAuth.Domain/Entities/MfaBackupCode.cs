namespace ExoAuth.Domain.Entities;

/// <summary>
/// MFA backup code for account recovery when TOTP device is unavailable.
/// Format: XXXX-XXXX (8 characters, stored as hash)
/// </summary>
public sealed class MfaBackupCode : BaseEntity
{
    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    // Navigation
    public SystemUser User { get; private set; } = null!;

    private MfaBackupCode() { } // EF Core

    public static MfaBackupCode Create(Guid userId, string codeHash)
    {
        return new MfaBackupCode
        {
            UserId = userId,
            CodeHash = codeHash,
            IsUsed = false
        };
    }

    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        SetUpdated();
    }
}
