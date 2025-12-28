using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.MfaConfirm;

/// <summary>
/// Command to confirm MFA setup with the first TOTP code.
/// Enables MFA and returns backup codes.
/// </summary>
public sealed record MfaConfirmCommand(
    string Code
) : ICommand<MfaConfirmResponse>;
