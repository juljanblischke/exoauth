using System.Text.Json;
using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Announcements.Commands.UpdateEmailAnnouncement;

public sealed class UpdateEmailAnnouncementHandler(
    IAppDbContext dbContext
) : ICommandHandler<UpdateEmailAnnouncementCommand, EmailAnnouncementDetailDto>
{
    public async ValueTask<EmailAnnouncementDetailDto> Handle(
        UpdateEmailAnnouncementCommand request,
        CancellationToken cancellationToken)
    {
        var announcement = await dbContext.EmailAnnouncements
            .Include(a => a.CreatedByUser)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (announcement is null)
        {
            throw new EmailAnnouncementNotFoundException(request.Id);
        }

        if (!announcement.CanBeModified)
        {
            throw new EmailAnnouncementAlreadySentException(request.Id);
        }

        string? targetUserIdsJson = request.TargetType == EmailAnnouncementTarget.SelectedUsers
            ? JsonSerializer.Serialize(request.TargetUserIds)
            : null;

        announcement.Update(
            request.Subject,
            request.HtmlBody,
            request.PlainTextBody,
            request.TargetType,
            request.TargetPermission,
            targetUserIdsJson);

        await dbContext.SaveChangesAsync(cancellationToken);

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
            request.TargetUserIds,
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
