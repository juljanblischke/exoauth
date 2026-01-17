using System.Text.Json;
using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Messages;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Email.Announcements.Commands.SendEmailAnnouncement;

public sealed class SendEmailAnnouncementHandler(
    IAppDbContext dbContext,
    IMessageBus messageBus,
    ILogger<SendEmailAnnouncementHandler> logger
) : ICommandHandler<SendEmailAnnouncementCommand, EmailAnnouncementDto>
{
    private const string EmailRoutingKey = "email.send";

    public async ValueTask<EmailAnnouncementDto> Handle(
        SendEmailAnnouncementCommand request,
        CancellationToken cancellationToken)
    {
        var announcement = await dbContext.EmailAnnouncements
            .Include(a => a.CreatedByUser)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (announcement is null)
        {
            throw new EmailAnnouncementNotFoundException(request.Id);
        }

        if (!announcement.IsDraft)
        {
            throw new EmailAnnouncementAlreadySentException(request.Id);
        }

        // Get recipient users based on target type
        var recipients = await GetRecipientsAsync(announcement, cancellationToken);

        if (recipients.Count == 0)
        {
            throw new EmailAnnouncementNoRecipientsException();
        }

        // Start sending
        announcement.StartSending(recipients.Count);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Announcement {AnnouncementId} started sending to {TotalRecipients} recipients",
            announcement.Id, recipients.Count);

        // Queue emails for each recipient
        // Note: EmailLog is created by EmailSendingService when processing the message
        foreach (var recipient in recipients)
        {
            var message = new SendEmailMessage(
                To: recipient.Email,
                Subject: announcement.Subject,
                TemplateName: "announcement",
                Language: recipient.PreferredLanguage,
                Variables: new Dictionary<string, string>(),
                RecipientUserId: recipient.Id,
                AnnouncementId: announcement.Id,
                HtmlBody: announcement.HtmlBody,
                PlainTextBody: announcement.PlainTextBody
            );

            await messageBus.PublishAsync(message, EmailRoutingKey, cancellationToken);
        }

        logger.LogInformation(
            "Announcement {AnnouncementId} queued {Count} emails for sending",
            announcement.Id, recipients.Count);

        var createdByName = announcement.CreatedByUser is not null
            ? $"{announcement.CreatedByUser.FirstName} {announcement.CreatedByUser.LastName}"
            : null;

        return new EmailAnnouncementDto(
            announcement.Id,
            announcement.Subject,
            announcement.TargetType,
            announcement.TargetPermission,
            announcement.TotalRecipients,
            announcement.SentCount,
            announcement.FailedCount,
            announcement.Progress,
            announcement.Status,
            announcement.CreatedByUserId,
            createdByName,
            announcement.SentAt,
            announcement.CreatedAt);
    }

    private async Task<List<SystemUser>> GetRecipientsAsync(
        Domain.Entities.EmailAnnouncement announcement,
        CancellationToken cancellationToken)
    {
        return announcement.TargetType switch
        {
            EmailAnnouncementTarget.AllUsers => await dbContext.SystemUsers
                .Where(u => u.IsActive && !u.IsAnonymized)
                .ToListAsync(cancellationToken),

            EmailAnnouncementTarget.ByPermission => await GetUsersByPermissionAsync(
                announcement.TargetPermission!, cancellationToken),

            EmailAnnouncementTarget.SelectedUsers => await GetSelectedUsersAsync(
                announcement.TargetUserIds, cancellationToken),

            _ => new List<SystemUser>()
        };
    }

    private async Task<List<SystemUser>> GetUsersByPermissionAsync(
        string permission,
        CancellationToken cancellationToken)
    {
        return await dbContext.SystemUsers
            .Where(u => u.IsActive && !u.IsAnonymized)
            .Where(u => dbContext.SystemUserPermissions
                .Any(up => up.SystemUserId == u.Id &&
                           dbContext.SystemPermissions.Any(p => p.Id == up.SystemPermissionId && p.Name == permission)))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<SystemUser>> GetSelectedUsersAsync(
        string? targetUserIds,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetUserIds))
            return new List<SystemUser>();

        var userIds = JsonSerializer.Deserialize<List<Guid>>(targetUserIds);
        if (userIds is null || userIds.Count == 0)
            return new List<SystemUser>();

        return await dbContext.SystemUsers
            .Where(u => userIds.Contains(u.Id) && u.IsActive && !u.IsAnonymized)
            .ToListAsync(cancellationToken);
    }
}
