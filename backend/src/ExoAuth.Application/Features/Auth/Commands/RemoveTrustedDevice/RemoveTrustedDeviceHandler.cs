using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.RemoveTrustedDevice;

public sealed class RemoveTrustedDeviceHandler : ICommandHandler<RemoveTrustedDeviceCommand>
{
    private readonly ITrustedDeviceService _trustedDeviceService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppDbContext _context;
    private readonly IAuditService _auditService;

    public RemoveTrustedDeviceHandler(
        ITrustedDeviceService trustedDeviceService,
        ICurrentUserService currentUserService,
        IAppDbContext context,
        IAuditService auditService)
    {
        _trustedDeviceService = trustedDeviceService;
        _currentUserService = currentUserService;
        _context = context;
        _auditService = auditService;
    }

    public async ValueTask<Unit> Handle(RemoveTrustedDeviceCommand command, CancellationToken ct)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException();

        var sessionId = _currentUserService.SessionId;

        // Get the trusted device and verify ownership
        var device = await _trustedDeviceService.GetByIdAsync(command.DeviceId, ct);

        if (device is null || device.UserId != userId)
        {
            throw new AuthException("DEVICE_NOT_FOUND", "Trusted device not found", 404);
        }

        // Check if trying to remove current device
        if (sessionId.HasValue)
        {
            var currentTrustedDeviceId = await _context.DeviceSessions
                .Where(s => s.Id == sessionId.Value)
                .Select(s => s.TrustedDeviceId)
                .FirstOrDefaultAsync(ct);

            if (currentTrustedDeviceId == command.DeviceId)
            {
                throw new AuthException("CANNOT_REMOVE_CURRENT_DEVICE", "Cannot remove the device you are currently using", 400);
            }
        }

        var deviceInfo = new { device.DeviceId, device.Name, device.Browser, device.OperatingSystem };

        await _trustedDeviceService.RemoveAsync(command.DeviceId, ct);

        await _auditService.LogWithContextAsync(
            AuditActions.TrustedDeviceRemoved,
            userId,
            null,
            "TrustedDevice",
            command.DeviceId,
            deviceInfo,
            ct
        );

        return Unit.Value;
    }
}
