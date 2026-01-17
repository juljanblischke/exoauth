using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Queries.GetDevices;

public sealed class GetDevicesHandler : IQueryHandler<GetDevicesQuery, List<DeviceDto>>
{
    private readonly IDeviceService _deviceService;
    private readonly ICurrentUserService _currentUserService;

    public GetDevicesHandler(
        IDeviceService deviceService,
        ICurrentUserService currentUserService)
    {
        _deviceService = deviceService;
        _currentUserService = currentUserService;
    }

    public async ValueTask<List<DeviceDto>> Handle(GetDevicesQuery query, CancellationToken ct)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        var currentDeviceId = _currentUserService.SessionId;

        var devices = await _deviceService.GetAllForUserAsync(userId, ct);

        return devices
            .Select(d => DeviceDto.FromEntity(d, currentDeviceId))
            .ToList();
    }
}
