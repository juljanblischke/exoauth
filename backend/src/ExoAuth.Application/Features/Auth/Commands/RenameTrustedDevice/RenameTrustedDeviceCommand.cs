using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RenameTrustedDevice;

/// <summary>
/// Command to rename a trusted device.
/// </summary>
/// <param name="DeviceId">The trusted device ID (primary key).</param>
/// <param name="Name">The new name for the device.</param>
public sealed record RenameTrustedDeviceCommand(Guid DeviceId, string Name) : ICommand<TrustedDeviceDto>;
