using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.RevokeUserSessions;

public sealed record RevokeUserSessionsCommand(
    Guid UserId
) : ICommand<RevokeUserSessionsResponse>;
