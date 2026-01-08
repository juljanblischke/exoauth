using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Announcements.Commands.CreateEmailAnnouncement;

public sealed class CreateEmailAnnouncementHandler(
    IAppDbContext dbContext
) : ICommandHandler<CreateEmailAnnouncementCommand, EmailAnnouncementDetailDto>
{
    public async ValueTask<EmailAnnouncementDetailDto> Handle(
        CreateEmailAnnouncementCommand request,
        CancellationToken cancellationToken)
    {
        EmailAnnouncement announcement = request.TargetType switch
        {
            EmailAnnouncementTarget.AllUsers => EmailAnnouncement.CreateForAllUsers(
                request.Subject,
                request.HtmlBody,
                request.CreatedByUserId,
                request.PlainTextBody),

            EmailAnnouncementTarget.ByPermission => EmailAnnouncement.CreateForPermission(
                request.Subject,
                request.HtmlBody,
                request.TargetPermission!,
                request.CreatedByUserId,
                request.PlainTextBody),

            EmailAnnouncementTarget.SelectedUsers => EmailAnnouncement.CreateForSelectedUsers(
                request.Subject,
                request.HtmlBody,
                JsonSerializer.Serialize(request.TargetUserIds),
                request.CreatedByUserId,
                request.PlainTextBody),

            _ => throw new ArgumentOutOfRangeException(nameof(request.TargetType))
        };

        dbContext.EmailAnnouncements.Add(announcement);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Load the creator for the response
        var createdByUser = await dbContext.SystemUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.CreatedByUserId, cancellationToken);

        var createdByName = createdByUser is not null
            ? $"{createdByUser.FirstName} {createdByUser.LastName}"
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
