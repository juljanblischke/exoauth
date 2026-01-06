using ExoAuth.Application.Features.Auth.Models;
using Fido2NetLib;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.PasskeyRegister;

/// <summary>
/// Command to complete passkey registration with the attestation response.
/// </summary>
public sealed record PasskeyRegisterCommand(
    string ChallengeId,
    AuthenticatorAttestationRawResponse AttestationResponse,
    string? Name = null
) : ICommand<PasskeyDto>;
