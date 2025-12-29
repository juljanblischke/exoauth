using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Queries.GetUserSessions;

public sealed record GetUserSessionsQuery(
    Guid UserId
) : IQuery<List<DeviceSessionDto>>;
