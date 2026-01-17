using ExoAuth.Application.Features.Email.Models;
using Mediator;
using ExoAuth.Domain.Enums;

namespace ExoAuth.Application.Features.Email.Announcements.Queries.PreviewEmailAnnouncement;

/// <summary>
/// Query to preview an announcement and get estimated recipients.
/// </summary>
public sealed record PreviewEmailAnnouncementQuery(
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    EmailAnnouncementTarget TargetType,
    string? TargetPermission,
    List<Guid>? TargetUserIds
) : IQuery<AnnouncementPreviewDto>;
