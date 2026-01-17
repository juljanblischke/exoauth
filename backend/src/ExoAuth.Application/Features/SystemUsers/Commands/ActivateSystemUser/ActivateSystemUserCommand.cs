using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.ActivateSystemUser;

/// <summary>
/// Command to activate a previously deactivated system user.
/// </summary>
public sealed record ActivateSystemUserCommand(
    Guid Id
) : ICommand<bool>;
