using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Queries.GetTrustedDevices;

public sealed class GetTrustedDevicesHandler : IQueryHandler<GetTrustedDevicesQuery, List<TrustedDeviceDto>>
{
    private readonly ITrustedDeviceService _trustedDeviceService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppDbContext _context;

    public GetTrustedDevicesHandler(
        ITrustedDeviceService trustedDeviceService,
        ICurrentUserService currentUserService,
        IAppDbContext context)
    {
        _trustedDeviceService = trustedDeviceService;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async ValueTask<List<TrustedDeviceDto>> Handle(GetTrustedDevicesQuery query, CancellationToken ct)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException();

        var sessionId = _currentUserService.SessionId;

        // Get the current session's TrustedDeviceId to determine which device is "current"
        Guid? currentTrustedDeviceId = null;
        if (sessionId.HasValue)
        {
            currentTrustedDeviceId = await _context.DeviceSessions
                .Where(s => s.Id == sessionId.Value)
                .Select(s => s.TrustedDeviceId)
                .FirstOrDefaultAsync(ct);
        }

        var devices = await _trustedDeviceService.GetAllAsync(userId, ct);

        return devices.Select(d => new TrustedDeviceDto(
            Id: d.Id,
            DeviceId: d.DeviceId,
            Name: d.DisplayName,
            Browser: d.Browser,
            BrowserVersion: d.BrowserVersion,
            OperatingSystem: d.OperatingSystem,
            OsVersion: d.OsVersion,
            DeviceType: d.DeviceType,
            LastIpAddress: d.LastIpAddress,
            LastCountry: d.LastCountry,
            LastCity: d.LastCity,
            LocationDisplay: d.LocationDisplay,
            IsCurrent: d.Id == currentTrustedDeviceId,
            TrustedAt: d.TrustedAt,
            LastUsedAt: d.LastUsedAt
        )).ToList();
    }

    
}
