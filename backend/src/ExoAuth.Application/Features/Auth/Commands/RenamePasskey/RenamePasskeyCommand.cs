using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RenamePasskey;

/// <summary>
/// Command to rename a passkey.
/// </summary>
public sealed record RenamePasskeyCommand(
    Guid PasskeyId,
    string Name
) : ICommand<PasskeyDto>;
