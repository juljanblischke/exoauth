using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RemoveTrustedDevice;

/// <summary>
/// Command to remove a trusted device.
/// </summary>
/// <param name="DeviceId">The trusted device ID (primary key).</param>
public sealed record RemoveTrustedDeviceCommand(Guid DeviceId) : ICommand;
