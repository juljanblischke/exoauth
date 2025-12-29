namespace ExoAuth.Domain.Entities;

public sealed class SystemUser : BaseEntity
{
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public bool EmailVerified { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // MFA fields
    public bool MfaEnabled { get; private set; }
    public string? MfaSecret { get; private set; }
    public DateTime? MfaEnabledAt { get; private set; }

    // User preferences
    public string PreferredLanguage { get; private set; } = "en-US";

    // Lockout fields
    public DateTime? LockedUntil { get; private set; }
    public int FailedLoginAttempts { get; private set; }

    // Anonymization fields
    public bool IsAnonymized { get; private set; }
    public DateTime? AnonymizedAt { get; private set; }

    // Navigation properties
    private readonly List<SystemUserPermission> _permissions = new();
    public IReadOnlyCollection<SystemUserPermission> Permissions => _permissions.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<MfaBackupCode> _mfaBackupCodes = new();
    public IReadOnlyCollection<MfaBackupCode> MfaBackupCodes => _mfaBackupCodes.AsReadOnly();

    private SystemUser() { } // EF Core

    public static SystemUser Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        bool emailVerified = false)
    {
        return new SystemUser
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            EmailVerified = emailVerified
        };
    }

    public void Update(string? firstName = null, string? lastName = null, bool? isActive = null)
    {
        if (firstName is not null)
            FirstName = firstName;

        if (lastName is not null)
            LastName = lastName;

        if (isActive.HasValue)
            IsActive = isActive.Value;

        SetUpdated();
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        SetUpdated();
    }

    public void SetEmailVerified(bool verified = true)
    {
        EmailVerified = verified;
        SetUpdated();
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdated();
    }

    public void SetMfaSecret(string encryptedSecret)
    {
        MfaSecret = encryptedSecret;
        SetUpdated();
    }

    public void EnableMfa()
    {
        if (MfaEnabled)
            return;

        MfaEnabled = true;
        MfaEnabledAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void DisableMfa()
    {
        MfaEnabled = false;
        MfaSecret = null;
        MfaEnabledAt = null;
        SetUpdated();
    }

    public void SetPreferredLanguage(string language)
    {
        PreferredLanguage = language;
        SetUpdated();
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        SetUpdated();
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        SetUpdated();
    }

    public void Lock(DateTime? until = null)
    {
        LockedUntil = until;
        SetUpdated();
    }

    public void Unlock()
    {
        LockedUntil = null;
        FailedLoginAttempts = 0;
        SetUpdated();
    }

    public bool IsLocked => LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;

    public void Anonymize()
    {
        if (IsAnonymized) return;

        Email = $"anonymized_{Id:N}@deleted.local";
        FirstName = "Deleted";
        LastName = "User";
        PasswordHash = "ANONYMIZED";
        IsActive = false;
        IsAnonymized = true;
        AnonymizedAt = DateTime.UtcNow;

        // Clear MFA data
        MfaEnabled = false;
        MfaSecret = null;
        MfaEnabledAt = null;

        // Clear lockout data
        LockedUntil = null;
        FailedLoginAttempts = 0;

        SetUpdated();
    }

    public string FullName => $"{FirstName} {LastName}";
}
