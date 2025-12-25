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

    // Navigation properties
    private readonly List<SystemUserPermission> _permissions = new();
    public IReadOnlyCollection<SystemUserPermission> Permissions => _permissions.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

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

    public string FullName => $"{FirstName} {LastName}";
}
