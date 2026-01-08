using ExoAuth.Application.Features.Email.Models;
using Mediator;
using ExoAuth.Domain.Enums;

namespace ExoAuth.Application.Features.Email.Announcements.Commands.CreateEmailAnnouncement;

/// <summary>
/// Command to create a new email announcement.
/// </summary>
public sealed record CreateEmailAnnouncementCommand(
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    EmailAnnouncementTarget TargetType,
    string? TargetPermission,
    List<Guid>? TargetUserIds,
    Guid CreatedByUserId
) : ICommand<EmailAnnouncementDetailDto>;
