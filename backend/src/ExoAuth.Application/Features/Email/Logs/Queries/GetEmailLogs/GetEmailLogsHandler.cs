using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Email.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Logs.Queries.GetEmailLogs;

public sealed class GetEmailLogsHandler : IQueryHandler<GetEmailLogsQuery, CursorPagedList<EmailLogDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetEmailLogsHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<CursorPagedList<EmailLogDto>> Handle(GetEmailLogsQuery query, CancellationToken ct)
    {
        var dbQuery = _dbContext.EmailLogs.AsQueryable();

        // Filter by status
        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Status == query.Status.Value);
        }

        // Filter by template name
        if (!string.IsNullOrWhiteSpace(query.TemplateName))
        {
            dbQuery = dbQuery.Where(x => x.TemplateName == query.TemplateName);
        }

        // Search in recipient email
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLower();
            dbQuery = dbQuery.Where(x =>
                x.RecipientEmail.ToLower().Contains(searchLower) ||
                x.Subject.ToLower().Contains(searchLower));
        }

        // Filter by recipient user
        if (query.RecipientUserId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.RecipientUserId == query.RecipientUserId.Value);
        }

        // Filter by announcement
        if (query.AnnouncementId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.AnnouncementId == query.AnnouncementId.Value);
        }

        // Filter by date range
        if (query.FromDate.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CreatedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CreatedAt <= query.ToDate.Value);
        }

        // Apply cursor pagination
        var cursorData = CursorPagedList<EmailLogDto>.DecodeCursorData(query.Cursor);

        // Parse sort
        var (sortField, sortDirection) = ParseSort(query.Sort);
        var isDescending = sortDirection == "desc";

        // Apply cursor filter based on sort field
        if (cursorData?.Id != null && cursorData?.Timestamp != null)
        {
            dbQuery = isDescending
                ? dbQuery.Where(x => x.CreatedAt < cursorData.Timestamp || (x.CreatedAt == cursorData.Timestamp && x.Id.CompareTo(cursorData.Id.Value) < 0))
                : dbQuery.Where(x => x.CreatedAt > cursorData.Timestamp || (x.CreatedAt == cursorData.Timestamp && x.Id.CompareTo(cursorData.Id.Value) > 0));
        }

        // Apply sorting
        dbQuery = isDescending
            ? dbQuery.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            : dbQuery.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id);

        // Execute query
        var items = await dbQuery
            .Take(query.Limit + 1)
            .Select(x => new EmailLogDto(
                x.Id,
                x.RecipientUserId,
                // Use joined user's email for consistency with audit logs (shows anonymized email)
                x.RecipientUser != null ? x.RecipientUser.Email : x.RecipientEmail,
                x.RecipientUser != null ? x.RecipientUser.FirstName + " " + x.RecipientUser.LastName : null,
                x.Subject,
                x.TemplateName,
                x.Language,
                x.Status,
                x.RetryCount,
                x.LastError,
                x.SentViaProviderId,
                x.SentViaProvider != null ? x.SentViaProvider.Name : null,
                x.QueuedAt,
                x.SentAt,
                x.FailedAt,
                x.MovedToDlqAt,
                x.AnnouncementId,
                x.CreatedAt))
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
            nextCursor = CursorPagedList<EmailLogDto>.EncodeCursor(cursorValue);
        }

        return CursorPagedList<EmailLogDto>.FromItems(resultItems, nextCursor, hasMore, query.Limit);
    }

    private static (string field, string direction) ParseSort(string sort)
    {
        var parts = sort.Split(':');
        var field = parts.Length > 0 ? parts[0] : "createdAt";
        var direction = parts.Length > 1 ? parts[1].ToLower() : "desc";
        return (field, direction);
    }
}
