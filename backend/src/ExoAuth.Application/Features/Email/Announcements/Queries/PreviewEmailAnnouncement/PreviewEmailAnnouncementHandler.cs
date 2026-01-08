using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Announcements.Queries.PreviewEmailAnnouncement;

public sealed class PreviewEmailAnnouncementHandler(
    IAppDbContext dbContext
) : IQueryHandler<PreviewEmailAnnouncementQuery, AnnouncementPreviewDto>
{
    public async ValueTask<AnnouncementPreviewDto> Handle(
        PreviewEmailAnnouncementQuery request,
        CancellationToken cancellationToken)
    {
        var estimatedRecipients = await CalculateEstimatedRecipientsAsync(request, cancellationToken);

        return new AnnouncementPreviewDto(
            request.Subject,
            request.HtmlBody,
            request.PlainTextBody,
            estimatedRecipients);
    }

    private async Task<int> CalculateEstimatedRecipientsAsync(
        PreviewEmailAnnouncementQuery request,
        CancellationToken cancellationToken)
    {
        return request.TargetType switch
        {
            EmailAnnouncementTarget.AllUsers => await dbContext.SystemUsers
                .CountAsync(u => u.IsActive && !u.IsAnonymized, cancellationToken),

            EmailAnnouncementTarget.ByPermission when !string.IsNullOrWhiteSpace(request.TargetPermission) =>
                await CountUsersByPermissionAsync(request.TargetPermission, cancellationToken),

            EmailAnnouncementTarget.SelectedUsers when request.TargetUserIds is not null =>
                await CountValidSelectedUsersAsync(request.TargetUserIds, cancellationToken),

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

    private async Task<int> CountValidSelectedUsersAsync(
        List<Guid> userIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.SystemUsers
            .Where(u => u.IsActive && !u.IsAnonymized && userIds.Contains(u.Id))
            .CountAsync(cancellationToken);
    }
}
