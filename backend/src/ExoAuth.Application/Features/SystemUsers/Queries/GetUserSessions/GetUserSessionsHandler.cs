using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Queries.GetUserSessions;

public sealed class GetUserSessionsHandler : IQueryHandler<GetUserSessionsQuery, List<DeviceSessionDto>>
{
    private readonly IAppDbContext _context;

    public GetUserSessionsHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async ValueTask<List<DeviceSessionDto>> Handle(GetUserSessionsQuery query, CancellationToken ct)
    {
        // Verify user exists
        var userExists = await _context.SystemUsers
            .AnyAsync(u => u.Id == query.UserId, ct);

        if (!userExists)
        {
            throw new SystemUserNotFoundException(query.UserId);
        }

        var sessions = await _context.DeviceSessions
            .Where(s => s.UserId == query.UserId && !s.IsRevoked)
            .OrderByDescending(s => s.LastActivityAt)
            .Select(s => new DeviceSessionDto(
                s.Id,
                s.DeviceId,
                s.DisplayName,
                s.DeviceName,
                s.Browser,
                s.BrowserVersion,
                s.OperatingSystem,
                s.OsVersion,
                s.DeviceType,
                s.IpAddress,
                s.Country,
                s.CountryCode,
                s.City,
                s.LocationDisplay,
                s.IsTrusted,
                false, // IsCurrent is always false for admin view
                s.LastActivityAt,
                s.CreatedAt
            ))
            .ToListAsync(ct);

        return sessions;
    }
}
