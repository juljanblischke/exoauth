using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.MfaVerify;

/// <summary>
/// Command to verify MFA code during login.
/// Accepts either a TOTP code or a backup code.
/// </summary>
public sealed record MfaVerifyCommand(
    string MfaToken,
    string Code,
    string? DeviceId,
    string? DeviceFingerprint,
    string? UserAgent,
    string? IpAddress,
    bool RememberMe = false
) : ICommand<AuthResponse>;
