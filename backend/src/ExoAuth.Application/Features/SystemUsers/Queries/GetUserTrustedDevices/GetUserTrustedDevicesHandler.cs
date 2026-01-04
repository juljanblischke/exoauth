using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Queries.GetUserTrustedDevices;

public sealed class GetUserTrustedDevicesHandler : IQueryHandler<GetUserTrustedDevicesQuery, List<TrustedDeviceDto>>
{
    private readonly ITrustedDeviceService _trustedDeviceService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppDbContext _context;

    public GetUserTrustedDevicesHandler(
        ITrustedDeviceService trustedDeviceService,
        ICurrentUserService currentUserService,
        IAppDbContext context)
    {
        _trustedDeviceService = trustedDeviceService;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async ValueTask<List<TrustedDeviceDto>> Handle(GetUserTrustedDevicesQuery query, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        // Verify the user exists
        var userExists = await _context.SystemUsers.AnyAsync(u => u.Id == query.UserId, ct);
        if (!userExists)
        {
            throw new SystemUserNotFoundException(query.UserId);
        }

        var devices = await _trustedDeviceService.GetAllAsync(query.UserId, ct);

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
            IsCurrent: false, // Admin view - never current
            TrustedAt: d.TrustedAt,
            LastUsedAt: d.LastUsedAt
        )).ToList();
    }
}
