namespace ExoAuth.Application.Features.SystemUsers.Models;

/// <summary>
/// System user information for list responses.
/// </summary>
public sealed record SystemUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    bool IsActive,
    bool EmailVerified,
    bool MfaEnabled,
    bool IsLocked,
    bool IsAnonymized,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>
/// System user with permissions for detail responses.
/// </summary>
public sealed record SystemUserDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    bool IsActive,
    bool EmailVerified,
    bool MfaEnabled,
    DateTime? MfaEnabledAt,
    string PreferredLanguage,
    bool IsLocked,
    DateTime? LockedUntil,
    int FailedLoginAttempts,
    bool IsAnonymized,
    DateTime? AnonymizedAt,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<PermissionDto> Permissions
);

/// <summary>
/// Permission information.
/// </summary>
public sealed record PermissionDto(
    Guid Id,
    string Name,
    string Description,
    string Category
);

/// <summary>
/// System invite information.
/// </summary>
public sealed record SystemInviteDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    DateTime ExpiresAt,
    DateTime CreatedAt
);
