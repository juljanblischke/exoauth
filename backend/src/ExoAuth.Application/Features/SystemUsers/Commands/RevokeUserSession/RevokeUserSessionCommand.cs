using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.RevokeUserSession;

public sealed record RevokeUserSessionCommand(
    Guid UserId,
    Guid SessionId
) : ICommand<RevokeUserSessionResponse>;
