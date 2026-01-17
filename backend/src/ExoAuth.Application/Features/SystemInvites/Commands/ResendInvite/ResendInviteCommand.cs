using ExoAuth.Application.Features.SystemInvites.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemInvites.Commands.ResendInvite;

/// <summary>
/// Command to resend a system invite email.
/// </summary>
public sealed record ResendInviteCommand(Guid Id) : ICommand<SystemInviteListDto>;
