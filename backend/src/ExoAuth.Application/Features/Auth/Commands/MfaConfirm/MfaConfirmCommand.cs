using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.MfaConfirm;

/// <summary>
/// Command to confirm MFA setup with the first TOTP code.
/// Enables MFA and returns backup codes.
/// Supports dual-mode authentication:
/// - JWT auth: For users already logged in who want to enable MFA
/// - SetupToken: For forced MFA setup during login/registration flow (returns tokens)
/// </summary>
public sealed record MfaConfirmCommand(
    string Code,
    string? SetupToken = null,
    // Device info - only needed for forced setup flow to create session
    string? DeviceId = null,
    string? DeviceFingerprint = null,
    string? UserAgent = null,
    string? IpAddress = null
) : ICommand<MfaConfirmResponse>;
