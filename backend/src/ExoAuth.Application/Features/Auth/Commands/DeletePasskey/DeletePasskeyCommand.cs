using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.DeletePasskey;

/// <summary>
/// Command to delete a passkey.
/// </summary>
public sealed record DeletePasskeyCommand(
    Guid PasskeyId
) : ICommand<bool>;
