using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.RenameTrustedDevice;

public sealed class RenameTrustedDeviceHandler : ICommandHandler<RenameTrustedDeviceCommand, TrustedDeviceDto>
{
    private readonly ITrustedDeviceService _trustedDeviceService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppDbContext _context;
    private readonly IAuditService _auditService;

    public RenameTrustedDeviceHandler(
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

    public async ValueTask<TrustedDeviceDto> Handle(RenameTrustedDeviceCommand command, CancellationToken ct)
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

        var oldName = device.Name;

        await _trustedDeviceService.RenameAsync(command.DeviceId, command.Name, ct);

        // Refresh device to get updated name
        device = await _trustedDeviceService.GetByIdAsync(command.DeviceId, ct);

        await _auditService.LogWithContextAsync(
            AuditActions.TrustedDeviceRenamed,
            userId,
            null,
            "TrustedDevice",
            command.DeviceId,
            new { OldName = oldName, NewName = command.Name },
            ct
        );

        // Determine if this is the current device
        Guid? currentTrustedDeviceId = null;
        if (sessionId.HasValue)
        {
            currentTrustedDeviceId = await _context.DeviceSessions
                .Where(s => s.Id == sessionId.Value)
                .Select(s => s.TrustedDeviceId)
                .FirstOrDefaultAsync(ct);
        }

        return new TrustedDeviceDto(
            Id: device!.Id,
            DeviceId: device.DeviceId,
            Name: device.DisplayName,
            Browser: device.Browser,
            BrowserVersion: device.BrowserVersion,
            OperatingSystem: device.OperatingSystem,
            OsVersion: device.OsVersion,
            DeviceType: device.DeviceType,
            LastIpAddress: device.LastIpAddress,
            LastCountry: device.LastCountry,
            LastCity: device.LastCity,
            LocationDisplay: device.LocationDisplay,
            IsCurrent: device.Id == currentTrustedDeviceId,
            TrustedAt: device.TrustedAt,
            LastUsedAt: device.LastUsedAt
        );
    }
}
