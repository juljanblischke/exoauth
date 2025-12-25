using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Command to refresh an access token using a refresh token.
/// </summary>
public sealed record RefreshTokenCommand(
    string RefreshToken
) : ICommand<TokenResponse>;
