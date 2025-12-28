using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.MfaSetup;

/// <summary>
/// Command to start MFA setup for the current user.
/// Returns QR code and manual entry key for authenticator apps.
/// </summary>
public sealed record MfaSetupCommand() : ICommand<MfaSetupResponse>;
