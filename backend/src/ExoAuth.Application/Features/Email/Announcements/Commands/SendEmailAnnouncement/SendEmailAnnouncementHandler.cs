using System.Text.Json;
using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Email.Announcements.Commands.SendEmailAnnouncement;

public sealed class SendEmailAnnouncementHandler(
    IAppDbContext dbContext,
    ILogger<SendEmailAnnouncementHandler> logger
) : ICommandHandler<SendEmailAnnouncementCommand, EmailAnnouncementDto>
{
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

        // Calculate total recipients based on target type
        var totalRecipients = await CalculateTotalRecipientsAsync(announcement, cancellationToken);

        if (totalRecipients == 0)
        {
            throw new EmailAnnouncementNoRecipientsException();
        }

        // Start sending
        announcement.StartSending(totalRecipients);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Announcement {AnnouncementId} started sending to {TotalRecipients} recipients",
            announcement.Id, totalRecipients);

        // TODO: Queue announcement emails for background processing
        // This will be implemented in Phase 4 when email queue infrastructure is added

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

    private async Task<int> CalculateTotalRecipientsAsync(
        Domain.Entities.EmailAnnouncement announcement,
        CancellationToken cancellationToken)
    {
        return announcement.TargetType switch
        {
            EmailAnnouncementTarget.AllUsers => await dbContext.SystemUsers
                .CountAsync(u => u.IsActive && !u.IsAnonymized, cancellationToken),

            EmailAnnouncementTarget.ByPermission => await CountUsersByPermissionAsync(
                announcement.TargetPermission!, cancellationToken),

            EmailAnnouncementTarget.SelectedUsers => CountSelectedUsers(announcement.TargetUserIds),

            _ => 0
        };
    }

    private async Task<int> CountUsersByPermissionAsync(
        string permission,
        CancellationToken cancellationToken)
    {
        // Count users who have the specified permission via the join table
        return await dbContext.SystemUsers
            .Where(u => u.IsActive && !u.IsAnonymized)
            .Where(u => dbContext.SystemUserPermissions
                .Any(up => up.SystemUserId == u.Id &&
                           dbContext.SystemPermissions.Any(p => p.Id == up.SystemPermissionId && p.Name == permission)))
            .CountAsync(cancellationToken);
    }

    private static int CountSelectedUsers(string? targetUserIds)
    {
        if (string.IsNullOrWhiteSpace(targetUserIds))
            return 0;

        var userIds = JsonSerializer.Deserialize<List<Guid>>(targetUserIds);
        return userIds?.Count ?? 0;
    }
}
