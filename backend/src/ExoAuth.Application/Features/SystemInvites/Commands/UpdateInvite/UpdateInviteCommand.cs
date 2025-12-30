using ExoAuth.Application.Features.SystemInvites.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemInvites.Commands.UpdateInvite;

/// <summary>
/// Command to update a pending system invite.
/// </summary>
public sealed record UpdateInviteCommand(
    Guid Id,
    string? FirstName,
    string? LastName,
    List<Guid>? PermissionIds
) : ICommand<SystemInviteListDto>;
