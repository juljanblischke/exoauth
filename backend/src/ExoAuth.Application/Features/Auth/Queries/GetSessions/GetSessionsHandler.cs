using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Queries.GetSessions;

public sealed class GetSessionsHandler : IQueryHandler<GetSessionsQuery, List<DeviceSessionDto>>
{
    private readonly IDeviceSessionService _sessionService;
    private readonly ICurrentUserService _currentUserService;

    public GetSessionsHandler(
        IDeviceSessionService sessionService,
        ICurrentUserService currentUserService)
    {
        _sessionService = sessionService;
        _currentUserService = currentUserService;
    }

    public async ValueTask<List<DeviceSessionDto>> Handle(GetSessionsQuery query, CancellationToken ct)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException();

        var currentSessionId = _currentUserService.SessionId;

        var sessions = await _sessionService.GetActiveSessionsAsync(userId, ct);

        return sessions.Select(s => new DeviceSessionDto(
            Id: s.Id,
            DeviceId: s.DeviceId,
            DisplayName: s.DisplayName,
            DeviceName: s.DeviceName,
            Browser: s.Browser,
            BrowserVersion: s.BrowserVersion,
            OperatingSystem: s.OperatingSystem,
            OsVersion: s.OsVersion,
            DeviceType: s.DeviceType,
            IpAddress: s.IpAddress,
            Country: s.Country,
            CountryCode: s.CountryCode,
            City: s.City,
            LocationDisplay: s.LocationDisplay,
            IsTrusted: s.IsTrusted,
            IsCurrent: s.Id == currentSessionId,
            LastActivityAt: s.LastActivityAt,
            CreatedAt: s.CreatedAt
        )).ToList();
    }
}
