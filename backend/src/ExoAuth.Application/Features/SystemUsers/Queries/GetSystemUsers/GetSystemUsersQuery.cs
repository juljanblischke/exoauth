using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemUsers.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Queries.GetSystemUsers;

/// <summary>
/// Query to get paginated list of system users.
/// </summary>
public sealed record GetSystemUsersQuery(
    string? Cursor = null,
    int Limit = 20,
    string? Sort = null,
    string? Search = null,
    List<Guid>? PermissionIds = null
) : IQuery<CursorPagedList<SystemUserDto>>;
