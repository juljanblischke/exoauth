using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.ResetUserMfa;

public sealed record ResetUserMfaCommand(
    Guid UserId,
    string? Reason = null
) : ICommand<ResetUserMfaResponse>;
