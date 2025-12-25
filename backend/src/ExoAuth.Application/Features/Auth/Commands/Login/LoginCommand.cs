using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.Login;

/// <summary>
/// Command to login a user with email and password.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password
) : ICommand<AuthResponse>;
