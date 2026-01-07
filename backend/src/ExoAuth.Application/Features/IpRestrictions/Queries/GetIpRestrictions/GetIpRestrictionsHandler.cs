using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.IpRestrictions.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.IpRestrictions.Queries.GetIpRestrictions;

public sealed class GetIpRestrictionsHandler : IQueryHandler<GetIpRestrictionsQuery, CursorPagedList<IpRestrictionDto>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetIpRestrictionsHandler(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async ValueTask<CursorPagedList<IpRestrictionDto>> Handle(GetIpRestrictionsQuery query, CancellationToken ct)
    {
        var now = _dateTimeProvider.UtcNow;

        var dbQuery = _dbContext.IpRestrictions.AsQueryable();

        // Filter by type
        if (query.Type.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Type == query.Type.Value);
        }

        // Filter by source
        if (query.Source.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Source == query.Source.Value);
        }

        // Filter expired
        if (!query.IncludeExpired)
        {
            dbQuery = dbQuery.Where(x => x.ExpiresAt == null || x.ExpiresAt > now);
        }

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLower();
            dbQuery = dbQuery.Where(x =>
                x.IpAddress.ToLower().Contains(searchLower) ||
                x.Reason.ToLower().Contains(searchLower));
        }

        // Apply cursor pagination
        var cursorData = CursorPagedList<IpRestrictionDto>.DecodeCursorData(query.Cursor);

        // Parse sort
        var (sortField, sortDirection) = ParseSort(query.Sort);
        var isDescending = sortDirection == "desc";

        // Apply cursor filter
        if (cursorData?.Id != null && cursorData?.Timestamp != null)
        {
            dbQuery = ApplyCursorFilter(dbQuery, sortField, isDescending, cursorData.Timestamp.Value, cursorData.Id.Value);
        }

        // Apply sorting
        dbQuery = ApplySorting(dbQuery, sortField, isDescending);

        // Execute query
        var items = await dbQuery
            .Take(query.Limit + 1)
            .Select(x => new IpRestrictionDto(
                x.Id,
                x.IpAddress,
                x.Type,
                x.Reason,
                x.Source,
                x.ExpiresAt,
                x.CreatedAt,
                x.CreatedByUserId,
                x.CreatedByUser != null ? x.CreatedByUser.Email : null
            ))
            .ToListAsync(ct);

        var hasMore = items.Count > query.Limit;
        var resultItems = hasMore ? items.Take(query.Limit).ToList() : items;

        string? nextCursor = null;
        if (hasMore && resultItems.Count > 0)
        {
            var lastItem = resultItems[^1];
            var cursorValue = JsonSerializer.Serialize(new CursorData
            {
                Id = lastItem.Id,
                Timestamp = lastItem.CreatedAt
            });
            nextCursor = CursorPagedList<IpRestrictionDto>.EncodeCursor(cursorValue);
        }

        return CursorPagedList<IpRestrictionDto>.FromItems(resultItems, nextCursor, hasMore, query.Limit);
    }

    private static (string field, string direction) ParseSort(string sort)
    {
        var parts = sort.Split(':');
        var field = parts[0].ToLower();
        var direction = parts.Length > 1 ? parts[1].ToLower() : "desc";

        return field switch
        {
            "ipaddress" => ("ipAddress", direction),
            "expiresat" => ("expiresAt", direction),
            "createdat" => ("createdAt", direction),
            _ => ("createdAt", direction)
        };
    }

    private static IQueryable<Domain.Entities.IpRestriction> ApplyCursorFilter(
        IQueryable<Domain.Entities.IpRestriction> query,
        string sortField,
        bool isDescending,
        DateTime timestamp,
        Guid id)
    {
        return sortField switch
        {
            "expiresAt" => isDescending
                ? query.Where(x => x.ExpiresAt < timestamp || (x.ExpiresAt == timestamp && x.Id.CompareTo(id) < 0))
                : query.Where(x => x.ExpiresAt > timestamp || (x.ExpiresAt == timestamp && x.Id.CompareTo(id) > 0)),
            _ => isDescending
                ? query.Where(x => x.CreatedAt < timestamp || (x.CreatedAt == timestamp && x.Id.CompareTo(id) < 0))
                : query.Where(x => x.CreatedAt > timestamp || (x.CreatedAt == timestamp && x.Id.CompareTo(id) > 0))
        };
    }

    private static IQueryable<Domain.Entities.IpRestriction> ApplySorting(
        IQueryable<Domain.Entities.IpRestriction> query,
        string sortField,
        bool isDescending)
    {
        return (sortField, isDescending) switch
        {
            ("ipAddress", false) => query.OrderBy(x => x.IpAddress).ThenBy(x => x.Id),
            ("ipAddress", true) => query.OrderByDescending(x => x.IpAddress).ThenByDescending(x => x.Id),
            ("expiresAt", false) => query.OrderBy(x => x.ExpiresAt).ThenBy(x => x.Id),
            ("expiresAt", true) => query.OrderByDescending(x => x.ExpiresAt).ThenByDescending(x => x.Id),
            (_, false) => query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
            (_, true) => query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
        };
    }
}
