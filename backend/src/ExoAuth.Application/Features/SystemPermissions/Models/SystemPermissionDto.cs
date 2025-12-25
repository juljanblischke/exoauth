namespace ExoAuth.Application.Features.SystemPermissions.Models;

/// <summary>
/// System permission information for API responses.
/// </summary>
public sealed record SystemPermissionDto(
    Guid Id,
    string Name,
    string Description,
    string Category,
    DateTime CreatedAt
);

/// <summary>
/// Grouped permissions by category for UI display.
/// </summary>
public sealed record SystemPermissionGroupDto(
    string Category,
    IReadOnlyList<SystemPermissionDto> Permissions
);
