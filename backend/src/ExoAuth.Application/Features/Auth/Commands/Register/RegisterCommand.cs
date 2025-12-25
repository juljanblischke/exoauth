using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.Register;

/// <summary>
/// Command to register a new user. First user becomes SystemUser with all permissions.
/// </summary>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? OrganizationName = null
) : ICommand<AuthResponse>;
