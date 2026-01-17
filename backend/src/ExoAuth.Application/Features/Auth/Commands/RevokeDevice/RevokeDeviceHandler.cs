using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RevokeDevice;

public sealed class RevokeDeviceHandler : ICommandHandler<RevokeDeviceCommand, RevokeDeviceResponse>
{
    private readonly IDeviceService _deviceService;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public RevokeDeviceHandler(
        IDeviceService deviceService,
        IRevokedSessionService revokedSessionService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _deviceService = deviceService;
        _revokedSessionService = revokedSessionService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async ValueTask<RevokeDeviceResponse> Handle(RevokeDeviceCommand command, CancellationToken ct)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        var currentSessionId = _currentUserService.SessionId;

        // Check if trying to revoke current device
        if (command.DeviceId == currentSessionId)
        {
            return new RevokeDeviceResponse(false);
        }

        // Get the device and verify ownership
        var device = await _deviceService.GetByIdAsync(command.DeviceId, ct);

        if (device is null || device.UserId != userId)
        {
            return new RevokeDeviceResponse(false);
        }

        var revoked = await _deviceService.RevokeAsync(command.DeviceId, ct);

        if (revoked)
        {
            // Immediately invalidate access tokens for this device
            await _revokedSessionService.RevokeSessionAsync(command.DeviceId, ct);

            await _auditService.LogWithContextAsync(
                AuditActions.DeviceRevoked,
                userId,
                null,
                "Device",
                command.DeviceId,
                new { DeviceId = device.DeviceId, DeviceName = device.DisplayName },
                ct
            );
        }

        return new RevokeDeviceResponse(revoked);
    }
}
