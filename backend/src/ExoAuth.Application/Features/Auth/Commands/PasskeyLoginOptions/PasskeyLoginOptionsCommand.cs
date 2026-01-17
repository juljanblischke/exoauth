using Fido2NetLib;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.PasskeyLoginOptions;

/// <summary>
/// Command to get WebAuthn assertion options for passkey login.
/// </summary>
public sealed record PasskeyLoginOptionsCommand(
    string? Email = null
) : ICommand<PasskeyLoginOptionsResponse>;

/// <summary>
/// Response containing WebAuthn assertion options.
/// </summary>
public sealed record PasskeyLoginOptionsResponse(
    AssertionOptions Options,
    string ChallengeId
);
