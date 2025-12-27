using ExoAuth.Domain.Entities;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Repository for SystemUser operations.
/// </summary>
public interface ISystemUserRepository
{
    /// <summary>
    /// Gets a system user by ID.
    /// </summary>
    Task<SystemUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a system user by ID with their permissions loaded.
    /// </summary>
    Task<SystemUser?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a system user by email.
    /// </summary>
    Task<SystemUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a system user by email with their permissions loaded.
    /// </summary>
    Task<SystemUser?> GetByEmailWithPermissionsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is already in use by any user type.
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any system users exist.
    /// </summary>
    Task<bool> AnyExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new system user.
    /// </summary>
    Task<SystemUser> AddAsync(SystemUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a system user.
    /// </summary>
    Task UpdateAsync(SystemUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a system user.
    /// </summary>
    Task DeleteAsync(SystemUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated list of system users.
    /// </summary>
    Task<(List<SystemUser> Users, string? NextCursor, int Total)> GetPagedAsync(
        string? cursor,
        int limit,
        string? sortBy,
        string? search,
        List<Guid>? permissionIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of users who have a specific permission.
    /// </summary>
    Task<int> CountUsersWithPermissionAsync(string permissionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the permission names for a user.
    /// </summary>
    Task<List<string>> GetUserPermissionNamesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the permissions for a user (replaces all existing).
    /// </summary>
    Task SetUserPermissionsAsync(Guid userId, List<Guid> permissionIds, Guid? grantedBy = null, CancellationToken cancellationToken = default);
}
