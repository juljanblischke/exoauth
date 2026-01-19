using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.LoginWithMagicLink;

/// <summary>
/// Command to login a user with a magic link token.
/// </summary>
public sealed record LoginWithMagicLinkCommand(
    string Token,
    string? DeviceId = null,
    string? DeviceFingerprint = null,
    string? UserAgent = null,
    string? IpAddress = null,
    bool RememberMe = false
) : ICommand<AuthResponse>;
