using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RenameDevice;

public sealed record RenameDeviceCommand(Guid DeviceId, string? Name) : ICommand<DeviceDto?>;
