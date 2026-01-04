using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Queries.GetUserSessions;

public sealed class GetUserSessionsHandler : IQueryHandler<GetUserSessionsQuery, List<DeviceDto>>
{
    private readonly IAppDbContext _context;
    private readonly IDeviceService _deviceService;

    public GetUserSessionsHandler(
        IAppDbContext context,
        IDeviceService deviceService)
    {
        _context = context;
        _deviceService = deviceService;
    }

    public async ValueTask<List<DeviceDto>> Handle(GetUserSessionsQuery query, CancellationToken ct)
    {
        // Verify user exists
        var userExists = await _context.SystemUsers
            .AnyAsync(u => u.Id == query.UserId, ct);

        if (!userExists)
        {
            throw new SystemUserNotFoundException(query.UserId);
        }

        // Get all devices (which are now the sessions) for the user
        var devices = await _deviceService.GetAllForUserAsync(query.UserId, ct);

        return devices
            .OrderByDescending(d => d.LastUsedAt)
            .Select(d => DeviceDto.FromEntity(d))
            .ToList();
    }
}
