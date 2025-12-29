using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.AnonymizeUser;

public sealed record AnonymizeUserCommand(
    Guid UserId
) : ICommand<AnonymizeUserResponse>;
