using ExoAuth.Application.Features.SystemPermissions.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemPermissions.Queries.GetSystemPermissions;

/// <summary>
/// Query to get all available system permissions.
/// </summary>
public sealed record GetSystemPermissionsQuery(
    bool GroupByCategory = false
) : IQuery<IReadOnlyList<SystemPermissionDto>>;
