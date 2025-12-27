using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemInvites.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemInvites.Queries.GetSystemInvites;

/// <summary>
/// Query to get paginated list of system invites.
/// </summary>
public sealed record GetSystemInvitesQuery(
    string? Cursor = null,
    int Limit = 20,
    string? Search = null,
    List<string>? Statuses = null
) : IQuery<CursorPagedList<SystemInviteListDto>>;
