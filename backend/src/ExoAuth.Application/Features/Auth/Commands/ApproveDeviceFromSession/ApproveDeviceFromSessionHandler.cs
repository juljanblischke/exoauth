using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Enums;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.ApproveDeviceFromSession;

public sealed class ApproveDeviceFromSessionHandler : ICommandHandler<ApproveDeviceFromSessionCommand, ApproveDeviceFromSessionResponse>
{
    private readonly IDeviceService _deviceService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public ApproveDeviceFromSessionHandler(
        IDeviceService deviceService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _deviceService = deviceService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async ValueTask<ApproveDeviceFromSessionResponse> Handle(ApproveDeviceFromSessionCommand command, CancellationToken ct)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        // Get the pending device and verify ownership
        var device = await _deviceService.GetByIdAsync(command.PendingDeviceId, ct);

        if (device is null || device.UserId != userId)
        {
            return new ApproveDeviceFromSessionResponse(false, ErrorCodes.DeviceNotFound);
        }

        if (device.Status != DeviceStatus.PendingApproval)
        {
            return new ApproveDeviceFromSessionResponse(false, ErrorCodes.DeviceNotPending);
        }

        // Approve the device
        var approvedDevice = await _deviceService.ApproveFromSessionAsync(command.PendingDeviceId, userId, ct);

        if (approvedDevice is null)
        {
            return new ApproveDeviceFromSessionResponse(false, ErrorCodes.DeviceNotFound);
        }

        await _auditService.LogWithContextAsync(
            AuditActions.DeviceApprovedFromSession,
            userId,
            null,
            "Device",
            command.PendingDeviceId,
            new { DeviceId = approvedDevice.DeviceId, DeviceName = approvedDevice.DisplayName },
            ct
        );

        return new ApproveDeviceFromSessionResponse(true);
    }
}
