namespace ExoAuth.Domain.Entities;

/// <summary>
/// Represents a WebAuthn/FIDO2 passkey credential for passwordless authentication.
/// </summary>
public sealed class Passkey : BaseEntity
{
    public Guid UserId { get; private set; }

    // WebAuthn Credential Data
    public byte[] CredentialId { get; private set; } = null!;
    public byte[] PublicKey { get; private set; } = null!;
    public uint Counter { get; private set; }
    public string CredType { get; private set; } = null!;
    public Guid AaGuid { get; private set; }

    // Metadata
    public string Name { get; private set; } = null!;
    public DateTime? LastUsedAt { get; private set; }

    // Navigation
    public SystemUser? User { get; set; }

    private Passkey() { } // EF Core

    /// <summary>
    /// Creates a new passkey for a user.
    /// </summary>
    public static Passkey Create(
        Guid userId,
        byte[] credentialId,
        byte[] publicKey,
        uint counter,
        string credType,
        Guid aaGuid,
        string? name = null)
    {
        return new Passkey
        {
            UserId = userId,
            CredentialId = credentialId,
            PublicKey = publicKey,
            Counter = counter,
            CredType = credType,
            AaGuid = aaGuid,
            Name = name ?? GenerateDefaultName()
        };
    }

    /// <summary>
    /// Updates the signature counter after successful authentication.
    /// </summary>
    public void UpdateCounter(uint newCounter)
    {
        Counter = newCounter;
        LastUsedAt = DateTime.UtcNow;
        SetUpdated();
    }

    /// <summary>
    /// Renames the passkey.
    /// </summary>
    public void Rename(string name)
    {
        Name = name;
        SetUpdated();
    }

    /// <summary>
    /// Records that this passkey was used for authentication.
    /// </summary>
    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
        SetUpdated();
    }

    /// <summary>
    /// Generates a default name for a new passkey.
    /// </summary>
    private static string GenerateDefaultName()
    {
        return $"Passkey {DateTime.UtcNow:yyyy-MM-dd}";
    }
}
