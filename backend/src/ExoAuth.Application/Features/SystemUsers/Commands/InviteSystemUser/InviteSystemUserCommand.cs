using ExoAuth.Application.Features.SystemUsers.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.InviteSystemUser;

/// <summary>
/// Command to invite a new system user.
/// </summary>
public sealed record InviteSystemUserCommand(
    string Email,
    string FirstName,
    string LastName,
    List<Guid> PermissionIds
) : ICommand<SystemInviteDto>;
