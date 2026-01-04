using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.RemoveAllUserTrustedDevices;

/// <summary>
/// Command to remove all trusted devices for a specific user (admin action).
/// </summary>
/// <param name="UserId">The user ID.</param>
public sealed record RemoveAllUserTrustedDevicesCommand(Guid UserId) : ICommand<int>;
