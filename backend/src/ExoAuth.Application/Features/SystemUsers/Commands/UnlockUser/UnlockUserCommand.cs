using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.UnlockUser;

public sealed record UnlockUserCommand(
    Guid UserId,
    string? Reason = null
) : ICommand<UnlockUserResponse>;
