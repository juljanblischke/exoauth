using ExoAuth.Application.Features.Email.Models;
using Mediator;
using ExoAuth.Domain.Enums;

namespace ExoAuth.Application.Features.Email.Announcements.Commands.UpdateEmailAnnouncement;

/// <summary>
/// Command to update an existing email announcement.
/// </summary>
public sealed record UpdateEmailAnnouncementCommand(
    Guid Id,
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    EmailAnnouncementTarget TargetType,
    string? TargetPermission,
    List<Guid>? TargetUserIds
) : ICommand<EmailAnnouncementDetailDto>;
