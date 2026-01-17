using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.DeactivateSystemUser;

/// <summary>
/// Command to deactivate a system user.
/// </summary>
public sealed record DeactivateSystemUserCommand(
    Guid Id
) : ICommand<bool>;
