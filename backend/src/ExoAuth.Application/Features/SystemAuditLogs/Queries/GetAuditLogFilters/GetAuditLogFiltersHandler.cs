using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemAuditLogs.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemAuditLogs.Queries.GetAuditLogFilters;

public sealed class GetAuditLogFiltersHandler : IQueryHandler<GetAuditLogFiltersQuery, AuditLogFiltersDto>
{
    private readonly IAppDbContext _context;

    public GetAuditLogFiltersHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async ValueTask<AuditLogFiltersDto> Handle(
        GetAuditLogFiltersQuery query,
        CancellationToken ct)
    {
        // Get distinct actions
        var actions = await _context.SystemAuditLogs
            .Select(l => l.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(ct);

        // Get users who have audit log entries
        var userIds = await _context.SystemAuditLogs
            .Where(l => l.UserId != null)
            .Select(l => l.UserId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var users = await _context.SystemUsers
            .Where(u => userIds.Contains(u.Id))
            .OrderBy(u => u.Email)
            .Select(u => new AuditLogUserFilterDto(
                u.Id,
                u.Email,
                u.FullName
            ))
            .ToListAsync(ct);

        // Get date range
        var dateRange = await _context.SystemAuditLogs
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Earliest = g.Min(l => l.CreatedAt),
                Latest = g.Max(l => l.CreatedAt)
            })
            .FirstOrDefaultAsync(ct);

        return new AuditLogFiltersDto(
            Actions: actions,
            Users: users,
            EarliestDate: dateRange?.Earliest,
            LatestDate: dateRange?.Latest
        );
    }
}
