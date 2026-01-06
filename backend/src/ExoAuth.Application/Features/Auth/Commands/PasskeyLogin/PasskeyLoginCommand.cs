using ExoAuth.Application.Features.Auth.Models;
using Fido2NetLib;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.PasskeyLogin;

/// <summary>
/// Command to authenticate with a passkey.
/// </summary>
public sealed record PasskeyLoginCommand(
    string ChallengeId,
    AuthenticatorAssertionRawResponse AssertionResponse,
    string? DeviceId = null,
    string? DeviceFingerprint = null,
    string? IpAddress = null,
    string? UserAgent = null,
    bool RememberMe = false
) : ICommand<AuthResponse>;
