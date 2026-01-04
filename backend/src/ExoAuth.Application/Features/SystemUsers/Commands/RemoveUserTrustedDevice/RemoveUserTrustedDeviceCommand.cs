using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.RemoveUserTrustedDevice;

/// <summary>
/// Command to remove a trusted device for a specific user (admin action).
/// </summary>
/// <param name="UserId">The user ID.</param>
/// <param name="DeviceId">The trusted device ID (primary key).</param>
public sealed record RemoveUserTrustedDeviceCommand(Guid UserId, Guid DeviceId) : ICommand;
