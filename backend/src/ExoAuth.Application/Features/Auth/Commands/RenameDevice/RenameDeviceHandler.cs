using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RenameDevice;

public sealed class RenameDeviceHandler : ICommandHandler<RenameDeviceCommand, DeviceDto?>
{
    private readonly IDeviceService _deviceService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public RenameDeviceHandler(
        IDeviceService deviceService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _deviceService = deviceService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async ValueTask<DeviceDto?> Handle(RenameDeviceCommand command, CancellationToken ct)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        // Get the device and verify ownership
        var device = await _deviceService.GetByIdAsync(command.DeviceId, ct);

        if (device is null || device.UserId != userId)
        {
            return null;
        }

        var renamed = await _deviceService.RenameAsync(command.DeviceId, command.Name, ct);

        if (!renamed)
        {
            return null;
        }

        // Refresh device to get updated data
        device = await _deviceService.GetByIdAsync(command.DeviceId, ct);

        await _auditService.LogWithContextAsync(
            AuditActions.DeviceRenamed,
            userId,
            null,
            "Device",
            command.DeviceId,
            new { DeviceId = device!.DeviceId, NewName = command.Name },
            ct
        );

        return DeviceDto.FromEntity(device, _currentUserService.SessionId);
    }
}
