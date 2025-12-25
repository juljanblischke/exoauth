using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.DeleteSystemUser;

/// <summary>
/// Command to delete (deactivate) a system user.
/// </summary>
public sealed record DeleteSystemUserCommand(
    Guid Id
) : ICommand<bool>;
