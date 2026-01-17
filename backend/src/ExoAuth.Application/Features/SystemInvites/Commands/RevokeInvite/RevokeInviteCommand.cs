using ExoAuth.Application.Features.SystemInvites.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemInvites.Commands.RevokeInvite;

/// <summary>
/// Command to revoke a system invite.
/// </summary>
public sealed record RevokeInviteCommand(Guid Id) : ICommand<SystemInviteListDto>;
