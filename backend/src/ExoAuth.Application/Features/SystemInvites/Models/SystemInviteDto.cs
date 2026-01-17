namespace ExoAuth.Application.Features.SystemInvites.Models;

/// <summary>
/// Invited by user information.
/// </summary>
public sealed record InvitedByDto(
    Guid Id,
    string Email,
    string FullName
);

/// <summary>
/// System invite list item.
/// </summary>
public sealed record SystemInviteListDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    DateTime? AcceptedAt,
    DateTime? RevokedAt,
    DateTime? ResentAt,
    InvitedByDto InvitedBy
);

/// <summary>
/// System invite detail.
/// </summary>
public sealed record SystemInviteDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    DateTime? AcceptedAt,
    DateTime? RevokedAt,
    DateTime? ResentAt,
    InvitedByDto InvitedBy,
    IReadOnlyList<InvitePermissionDto> Permissions
);

/// <summary>
/// Permission info for invite.
/// </summary>
public sealed record InvitePermissionDto(
    string Name,
    string Description
);

/// <summary>
/// Public invite validation response.
/// </summary>
public sealed record InviteValidationDto(
    bool Valid,
    string? Email,
    string? FirstName,
    string? LastName,
    DateTime? ExpiresAt,
    InviterDto? InvitedBy,
    IReadOnlyList<InvitePermissionDto>? Permissions,
    string? ErrorCode,
    string? ErrorMessage
);

/// <summary>
/// Inviter info for public validation (minimal).
/// </summary>
public sealed record InviterDto(
    string FullName
);
