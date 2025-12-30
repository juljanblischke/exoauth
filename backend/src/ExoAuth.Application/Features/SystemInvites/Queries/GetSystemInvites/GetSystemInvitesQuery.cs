using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemInvites.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemInvites.Queries.GetSystemInvites;

/// <summary>
/// Query to get paginated list of system invites.
/// Sort options: email:asc, email:desc, firstName:asc, firstName:desc,
/// lastName:asc, lastName:desc, createdAt:asc, createdAt:desc (default),
/// expiresAt:asc, expiresAt:desc
/// </summary>
public sealed record GetSystemInvitesQuery(
    string? Cursor = null,
    int Limit = 20,
    string? Search = null,
    List<string>? Statuses = null,
    string Sort = "createdAt:desc",
    bool IncludeExpired = false,
    bool IncludeRevoked = false
) : IQuery<CursorPagedList<SystemInviteListDto>>;
