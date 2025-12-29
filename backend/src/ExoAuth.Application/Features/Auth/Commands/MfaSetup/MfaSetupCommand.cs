using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.MfaSetup;

/// <summary>
/// Command to start MFA setup for the current user.
/// Returns QR code and manual entry key for authenticator apps.
/// Supports dual-mode authentication:
/// - JWT auth: For users already logged in who want to enable MFA
/// - SetupToken: For forced MFA setup during login flow
/// </summary>
public sealed record MfaSetupCommand(
    string? SetupToken = null
) : ICommand<MfaSetupResponse>;
