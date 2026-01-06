using Fido2NetLib;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.PasskeyRegisterOptions;

/// <summary>
/// Command to get WebAuthn registration options for creating a new passkey.
/// Requires authenticated user.
/// </summary>
public sealed record PasskeyRegisterOptionsCommand : ICommand<PasskeyRegisterOptionsResponse>;

/// <summary>
/// Response containing WebAuthn registration options.
/// </summary>
public sealed record PasskeyRegisterOptionsResponse(
    CredentialCreateOptions Options,
    string ChallengeId
);
