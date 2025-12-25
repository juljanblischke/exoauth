using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Command to logout a user by revoking their refresh token.
/// </summary>
public sealed record LogoutCommand(
    string RefreshToken
) : ICommand<LogoutResponse>;
