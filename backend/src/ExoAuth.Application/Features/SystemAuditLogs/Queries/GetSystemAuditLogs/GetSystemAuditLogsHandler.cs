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
            .Include(l => l.TargetUser)
            .AsNoTracking()
            .AsQueryable();

        // Apply filters
        if (query.Actions is { Count: > 0 })
        {
            queryable = queryable.Where(l => query.Actions.Contains(l.Action));
        }

        // InvolvedUserIds: find logs where user is actor OR target
        if (query.InvolvedUserIds is { Count: > 0 })
        {
            queryable = queryable.Where(l =>
                (l.UserId.HasValue && query.InvolvedUserIds.Contains(l.UserId.Value)) ||
                (l.TargetUserId.HasValue && query.InvolvedUserIds.Contains(l.TargetUserId.Value))
            );
        }

        // Search: search in actor and target user email/name
        // Note: FullName is a computed C# property, so we search FirstName and LastName separately
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLowerInvariant();
            queryable = queryable.Where(l =>
                (l.User != null && (
                    l.User.Email.ToLower().Contains(searchLower) ||
                    l.User.FirstName.ToLower().Contains(searchLower) ||
                    l.User.LastName.ToLower().Contains(searchLower)
                )) ||
                (l.TargetUser != null && (
                    l.TargetUser.Email.ToLower().Contains(searchLower) ||
                    l.TargetUser.FirstName.ToLower().Contains(searchLower) ||
                    l.TargetUser.LastName.ToLower().Contains(searchLower)
                ))
            );
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
        // Note: FullName is computed in C# (FirstName + LastName) so we must compute it here
        var logs = await queryable
            .Take(limit + 1)
            .Select(l => new SystemAuditLogDto(
                l.Id,
                l.UserId,
                l.User != null ? l.User.Email : null,
                l.User != null ? l.User.FirstName + " " + l.User.LastName : null,
                l.TargetUserId,
                l.TargetUser != null ? l.TargetUser.Email : null,
                l.TargetUser != null ? l.TargetUser.FirstName + " " + l.TargetUser.LastName : null,
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
