using ExoAuth.Application.Features.SystemUsers.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.UpdateSystemUser;

/// <summary>
/// Command to update a system user's profile.
/// </summary>
public sealed record UpdateSystemUserCommand(
    Guid Id,
    string? FirstName = null,
    string? LastName = null,
    bool? IsActive = null
) : ICommand<SystemUserDto>;
