using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RegenerateBackupCodes;

/// <summary>
/// Command to regenerate MFA backup codes.
/// Requires current TOTP code for verification.
/// </summary>
public sealed record RegenerateBackupCodesCommand(
    string Code
) : ICommand<RegenerateBackupCodesResponse>;
