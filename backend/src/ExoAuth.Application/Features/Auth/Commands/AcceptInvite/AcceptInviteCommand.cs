using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.AcceptInvite;

/// <summary>
/// Command to accept an invitation and create a user account.
/// </summary>
public sealed record AcceptInviteCommand(
    string Token,
    string Password,
    string Language = "en",
    string? DeviceId = null,
    string? DeviceFingerprint = null,
    string? UserAgent = null,
    string? IpAddress = null
) : ICommand<AuthResponse>;
