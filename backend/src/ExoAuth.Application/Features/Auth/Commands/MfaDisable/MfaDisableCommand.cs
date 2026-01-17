using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.MfaDisable;

/// <summary>
/// Command to disable MFA for the current user.
/// Requires current TOTP code for verification.
/// </summary>
public sealed record MfaDisableCommand(
    string Code
) : ICommand<MfaDisableResponse>;
