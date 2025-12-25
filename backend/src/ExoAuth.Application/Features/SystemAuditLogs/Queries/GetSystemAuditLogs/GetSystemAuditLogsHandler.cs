using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemAuditLogs.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemAuditLogs.Queries.GetSystemAuditLogs;

public sealed class GetSystemAuditLogsHandler : IQueryHandler<GetSystemAuditLogsQuery, CursorPagedList<SystemAuditLogDto>>
{
    private readonly IAppDbContext _context;

    public GetSystemAuditLogsHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async ValueTask<CursorPagedList<SystemAuditLogDto>> Handle(
        GetSystemAuditLogsQuery query,
        CancellationToken ct)
    {
        var limit = Math.Clamp(query.Limit, 1, 100);

        var queryable = _context.SystemAuditLogs
            .Include(l => l.User)
            .AsNoTracking()
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            queryable = queryable.Where(l => l.Action == query.Action);
        }

        if (query.UserId.HasValue)
        {
            queryable = queryable.Where(l => l.UserId == query.UserId.Value);
        }

        if (query.From.HasValue)
        {
            queryable = queryable.Where(l => l.CreatedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            queryable = queryable.Where(l => l.CreatedAt <= query.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            queryable = queryable.Where(l => l.EntityType == query.EntityType);
        }

        if (query.EntityId.HasValue)
        {
            queryable = queryable.Where(l => l.EntityId == query.EntityId.Value);
        }

        // Apply cursor pagination
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var cursorData = CursorPagedList<SystemAuditLogDto>.DecodeCursorData(query.Cursor);
            if (cursorData?.Timestamp.HasValue == true)
            {
                queryable = queryable.Where(l => l.CreatedAt < cursorData.Timestamp.Value);
            }
        }

        // Apply sorting (default: newest first)
        var sortParts = query.Sort?.Split(':') ?? new[] { "createdAt", "desc" };
        var sortField = sortParts[0].ToLowerInvariant();
        var sortDirection = sortParts.Length > 1 ? sortParts[1].ToLowerInvariant() : "desc";

        queryable = sortField switch
        {
            "action" => sortDirection == "asc"
                ? queryable.OrderBy(l => l.Action)
                : queryable.OrderByDescending(l => l.Action),
            _ => sortDirection == "asc"
                ? queryable.OrderBy(l => l.CreatedAt)
                : queryable.OrderByDescending(l => l.CreatedAt)
        };

        // Fetch one extra to determine if there are more
        var logs = await queryable
            .Take(limit + 1)
            .Select(l => new SystemAuditLogDto(
                l.Id,
                l.UserId,
                l.User != null ? l.User.Email : null,
                l.User != null ? l.User.FullName : null,
                l.Action,
                l.EntityType,
                l.EntityId,
                l.IpAddress,
                l.UserAgent,
                l.Details,
                l.CreatedAt
            ))
            .ToListAsync(ct);

        var hasMore = logs.Count > limit;
        var items = hasMore ? logs.Take(limit).ToList() : logs;

        string? nextCursor = null;
        if (hasMore && items.Count > 0)
        {
            var lastItem = items[^1];
            var cursorData = new CursorData
            {
                Id = lastItem.Id,
                Timestamp = lastItem.CreatedAt
            };
            nextCursor = CursorPagedList<SystemAuditLogDto>.EncodeCursor(
                JsonSerializer.Serialize(cursorData)
            );
        }

        return CursorPagedList<SystemAuditLogDto>.Create(
            items: logs,
            pageSize: limit,
            cursorSelector: l => JsonSerializer.Serialize(new CursorData
            {
                Id = l.Id,
                Timestamp = l.CreatedAt
            }),
            currentCursor: query.Cursor
        );
    }
}
