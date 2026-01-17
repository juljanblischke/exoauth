using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.IpRestrictions.Models;
using ExoAuth.Domain.Enums;
using Mediator;

namespace ExoAuth.Application.Features.IpRestrictions.Queries.GetIpRestrictions;

/// <summary>
/// Query to get paginated list of IP restrictions.
/// </summary>
public sealed record GetIpRestrictionsQuery(
    string? Cursor = null,
    int Limit = 20,
    IpRestrictionType? Type = null,
    IpRestrictionSource? Source = null,
    bool IncludeExpired = false,
    string? Search = null,
    string Sort = "createdAt:desc"
) : IQuery<CursorPagedList<IpRestrictionDto>>;
