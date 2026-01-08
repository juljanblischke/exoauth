using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Dlq.Queries.GetDlqEmails;

public sealed class GetDlqEmailsHandler : IQueryHandler<GetDlqEmailsQuery, CursorPagedList<EmailLogDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetDlqEmailsHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<CursorPagedList<EmailLogDto>> Handle(GetDlqEmailsQuery query, CancellationToken ct)
    {
        var dbQuery = _dbContext.EmailLogs
            .Where(x => x.Status == EmailStatus.InDlq);

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLower();
            dbQuery = dbQuery.Where(x =>
                x.RecipientEmail.ToLower().Contains(searchLower) ||
                x.Subject.ToLower().Contains(searchLower));
        }

        // Apply cursor pagination
        var cursorData = CursorPagedList<EmailLogDto>.DecodeCursorData(query.Cursor);

        // Parse sort
        var (sortField, sortDirection) = ParseSort(query.Sort);
        var isDescending = sortDirection == "desc";

        // Apply cursor filter
        if (cursorData?.Id != null && cursorData?.Timestamp != null)
        {
            dbQuery = isDescending
                ? dbQuery.Where(x => x.MovedToDlqAt < cursorData.Timestamp || (x.MovedToDlqAt == cursorData.Timestamp && x.Id.CompareTo(cursorData.Id.Value) < 0))
                : dbQuery.Where(x => x.MovedToDlqAt > cursorData.Timestamp || (x.MovedToDlqAt == cursorData.Timestamp && x.Id.CompareTo(cursorData.Id.Value) > 0));
        }

        // Apply sorting
        dbQuery = isDescending
            ? dbQuery.OrderByDescending(x => x.MovedToDlqAt).ThenByDescending(x => x.Id)
            : dbQuery.OrderBy(x => x.MovedToDlqAt).ThenBy(x => x.Id);

        // Execute query
        var items = await dbQuery
            .Take(query.Limit + 1)
            .Select(x => new EmailLogDto(
                x.Id,
                x.RecipientUserId,
                x.RecipientEmail,
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
                Timestamp = lastItem.MovedToDlqAt ?? lastItem.CreatedAt
            });
            nextCursor = CursorPagedList<EmailLogDto>.EncodeCursor(cursorValue);
        }

        return CursorPagedList<EmailLogDto>.FromItems(resultItems, nextCursor, hasMore, query.Limit);
    }

    private static (string field, string direction) ParseSort(string sort)
    {
        var parts = sort.Split(':');
        var direction = parts.Length > 1 ? parts[1].ToLower() : "desc";
        return ("movedToDlqAt", direction);
    }
}
