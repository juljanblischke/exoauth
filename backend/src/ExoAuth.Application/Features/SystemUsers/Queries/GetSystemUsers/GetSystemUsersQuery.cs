using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemUsers.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Queries.GetSystemUsers;

/// <summary>
/// Query to get paginated list of system users.
/// </summary>
/// <param name="Cursor">Pagination cursor.</param>
/// <param name="Limit">Number of items per page.</param>
/// <param name="Sort">Sort field and direction (e.g., "email:asc").</param>
/// <param name="Search">Search term for email, firstName, lastName.</param>
/// <param name="PermissionIds">Filter by users having ALL these permissions.</param>
/// <param name="IsActive">Filter by active status.</param>
/// <param name="IsAnonymized">Filter by anonymized status. Default: false (hide anonymized users).</param>
/// <param name="IsLocked">Filter by locked status.</param>
/// <param name="MfaEnabled">Filter by MFA enabled status.</param>
public sealed record GetSystemUsersQuery(
    string? Cursor = null,
    int Limit = 20,
    string? Sort = null,
    string? Search = null,
    List<Guid>? PermissionIds = null,
    bool? IsActive = null,
    bool? IsAnonymized = false,
    bool? IsLocked = null,
    bool? MfaEnabled = null
) : IQuery<CursorPagedList<SystemUserDto>>;
