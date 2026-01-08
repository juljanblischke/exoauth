using System.Text.Json;
using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Announcements.Queries.GetEmailAnnouncement;

public sealed class GetEmailAnnouncementHandler(
    IAppDbContext dbContext
) : IQueryHandler<GetEmailAnnouncementQuery, EmailAnnouncementDetailDto>
{
    public async ValueTask<EmailAnnouncementDetailDto> Handle(
        GetEmailAnnouncementQuery request,
        CancellationToken cancellationToken)
    {
        var announcement = await dbContext.EmailAnnouncements
            .Include(a => a.CreatedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (announcement is null)
        {
            throw new EmailAnnouncementNotFoundException(request.Id);
        }

        List<Guid>? targetUserIds = null;
        if (!string.IsNullOrWhiteSpace(announcement.TargetUserIds))
        {
            targetUserIds = JsonSerializer.Deserialize<List<Guid>>(announcement.TargetUserIds);
        }

        var createdByName = announcement.CreatedByUser is not null
            ? $"{announcement.CreatedByUser.FirstName} {announcement.CreatedByUser.LastName}"
            : null;

        return new EmailAnnouncementDetailDto(
            announcement.Id,
            announcement.Subject,
            announcement.HtmlBody,
            announcement.PlainTextBody,
            announcement.TargetType,
            announcement.TargetPermission,
            targetUserIds,
            announcement.TotalRecipients,
            announcement.SentCount,
            announcement.FailedCount,
            announcement.Progress,
            announcement.Status,
            announcement.CreatedByUserId,
            createdByName,
            announcement.SentAt,
            announcement.CreatedAt,
            announcement.UpdatedAt);
    }
}
