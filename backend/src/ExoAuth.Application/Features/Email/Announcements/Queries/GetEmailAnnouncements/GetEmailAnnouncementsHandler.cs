using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Announcements.Queries.GetEmailAnnouncements;

public sealed class GetEmailAnnouncementsHandler(
    IAppDbContext dbContext
) : IQueryHandler<GetEmailAnnouncementsQuery, CursorPagedList<EmailAnnouncementDto>>
{
    public async ValueTask<CursorPagedList<EmailAnnouncementDto>> Handle(
        GetEmailAnnouncementsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.EmailAnnouncements
            .Include(a => a.CreatedByUser)
            .AsNoTracking();

        // Apply status filter
        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(a => a.Subject.ToLower().Contains(searchLower));
        }

        // Parse sort
        var (sortField, sortDirection) = ParseSort(request.Sort);
        var isDescending = sortDirection == "desc";

        // Apply cursor pagination
        var cursorData = CursorPagedList<EmailAnnouncementDto>.DecodeCursorData(request.Cursor);

        if (cursorData?.Id != null && cursorData?.Timestamp != null)
        {
            query = ApplyCursorFilter(query, sortField, isDescending, cursorData.Timestamp.Value, cursorData.Id.Value);
        }

        // Apply sorting
        query = ApplySorting(query, sortField, isDescending);

        // Get one extra item to determine if there are more
        var items = await query
            .Take(request.Limit + 1)
            .Select(a => new EmailAnnouncementDto(
                a.Id,
                a.Subject,
                a.TargetType,
                a.TargetPermission,
                a.TotalRecipients,
                a.SentCount,
                a.FailedCount,
                a.TotalRecipients > 0 ? (double)a.SentCount / a.TotalRecipients * 100 : 0,
                a.Status,
                a.CreatedByUserId,
                a.CreatedByUser != null ? $"{a.CreatedByUser.FirstName} {a.CreatedByUser.LastName}" : null,
                a.SentAt,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > request.Limit;
        var resultItems = hasMore ? items.Take(request.Limit).ToList() : items;

        string? nextCursor = null;
        if (hasMore && resultItems.Count > 0)
        {
            var lastItem = resultItems[^1];
            var cursorValue = JsonSerializer.Serialize(new CursorData
            {
                Id = lastItem.Id,
                Timestamp = lastItem.CreatedAt
            });
            nextCursor = CursorPagedList<EmailAnnouncementDto>.EncodeCursor(cursorValue);
        }

        return CursorPagedList<EmailAnnouncementDto>.FromItems(resultItems, nextCursor, hasMore, request.Limit);
    }

    private static (string field, string direction) ParseSort(string sort)
    {
        var parts = sort.Split(':');
        var field = parts[0].ToLower();
        var direction = parts.Length > 1 ? parts[1].ToLower() : "desc";

        return field switch
        {
            "subject" => ("subject", direction),
            "status" => ("status", direction),
            "sentat" => ("sentAt", direction),
            "createdat" => ("createdAt", direction),
            _ => ("createdAt", direction)
        };
    }

    private static IQueryable<Domain.Entities.EmailAnnouncement> ApplyCursorFilter(
        IQueryable<Domain.Entities.EmailAnnouncement> query,
        string sortField,
        bool isDescending,
        DateTime timestamp,
        Guid id)
    {
        return sortField switch
        {
            "sentAt" => isDescending
                ? query.Where(a => a.SentAt < timestamp || (a.SentAt == timestamp && a.Id.CompareTo(id) < 0))
                : query.Where(a => a.SentAt > timestamp || (a.SentAt == timestamp && a.Id.CompareTo(id) > 0)),
            _ => isDescending
                ? query.Where(a => a.CreatedAt < timestamp || (a.CreatedAt == timestamp && a.Id.CompareTo(id) < 0))
                : query.Where(a => a.CreatedAt > timestamp || (a.CreatedAt == timestamp && a.Id.CompareTo(id) > 0))
        };
    }

    private static IQueryable<Domain.Entities.EmailAnnouncement> ApplySorting(
        IQueryable<Domain.Entities.EmailAnnouncement> query,
        string sortField,
        bool isDescending)
    {
        return (sortField, isDescending) switch
        {
            ("subject", false) => query.OrderBy(a => a.Subject).ThenBy(a => a.Id),
            ("subject", true) => query.OrderByDescending(a => a.Subject).ThenByDescending(a => a.Id),
            ("status", false) => query.OrderBy(a => a.Status).ThenBy(a => a.Id),
            ("status", true) => query.OrderByDescending(a => a.Status).ThenByDescending(a => a.Id),
            ("sentAt", false) => query.OrderBy(a => a.SentAt).ThenBy(a => a.Id),
            ("sentAt", true) => query.OrderByDescending(a => a.SentAt).ThenByDescending(a => a.Id),
            (_, false) => query.OrderBy(a => a.CreatedAt).ThenBy(a => a.Id),
            (_, true) => query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
        };
    }
}
