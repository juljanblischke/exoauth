namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for caching user permissions in Redis.
/// </summary>
public interface IPermissionCacheService
{
    /// <summary>
    /// Gets cached permissions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of permission names, or null if not cached.</returns>
    Task<List<string>?> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches permissions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="permissions">The permission names to cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetPermissionsAsync(Guid userId, List<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached permissions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached permissions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions for a user, using cache if available, otherwise fetching from the repository.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="fetchFromDb">Function to fetch permissions from the database.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of permission names.</returns>
    Task<List<string>> GetOrSetPermissionsAsync(
        Guid userId,
        Func<Task<List<string>>> fetchFromDb,
        CancellationToken cancellationToken = default);
}
