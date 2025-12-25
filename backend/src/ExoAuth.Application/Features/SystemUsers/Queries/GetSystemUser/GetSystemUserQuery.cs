using ExoAuth.Application.Features.SystemUsers.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Queries.GetSystemUser;

/// <summary>
/// Query to get a single system user with their permissions.
/// </summary>
public sealed record GetSystemUserQuery(
    Guid Id
) : IQuery<SystemUserDetailDto>;
