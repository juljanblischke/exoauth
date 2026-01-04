using ExoAuth.Application.Common.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RevokeDevice;

public sealed record RevokeDeviceCommand(Guid DeviceId) : ICommand<RevokeDeviceResponse>;

public sealed record RevokeDeviceResponse(bool Success);
