using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Commands.RemoveUserTrustedDevice;

public sealed class RemoveUserTrustedDeviceHandler : ICommandHandler<RemoveUserTrustedDeviceCommand>
{
    private readonly ITrustedDeviceService _trustedDeviceService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppDbContext _context;
    private readonly IAuditService _auditService;

    public RemoveUserTrustedDeviceHandler(
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

    public async ValueTask<Unit> Handle(RemoveUserTrustedDeviceCommand command, CancellationToken ct)
    {
        var adminUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException();

        // Verify the target user exists
        var userExists = await _context.SystemUsers.AnyAsync(u => u.Id == command.UserId, ct);
        if (!userExists)
        {
            throw new SystemUserNotFoundException(command.UserId);
        }

        // Get the trusted device and verify it belongs to the target user
        var device = await _trustedDeviceService.GetByIdAsync(command.DeviceId, ct);

        if (device is null || device.UserId != command.UserId)
        {
            throw new AuthException("DEVICE_NOT_FOUND", "Trusted device not found", 404);
        }

        var deviceInfo = new { device.DeviceId, device.Name, device.Browser, device.OperatingSystem };

        await _trustedDeviceService.RemoveAsync(command.DeviceId, ct);

        await _auditService.LogWithContextAsync(
            AuditActions.TrustedDeviceRemovedByAdmin,
            adminUserId,
            command.UserId,
            "TrustedDevice",
            command.DeviceId,
            deviceInfo,
            ct
        );

        return Unit.Value;
    }
}
