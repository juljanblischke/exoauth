using ExoAuth.Application.Features.SystemUsers.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.UpdatePermissions;

/// <summary>
/// Command to update a system user's permissions (replaces all existing).
/// </summary>
public sealed record UpdateSystemUserPermissionsCommand(
    Guid UserId,
    List<Guid> PermissionIds
) : ICommand<SystemUserDetailDto>;
