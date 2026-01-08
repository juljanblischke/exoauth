using ExoAuth.Application.Features.Email.Models;
using Mediator;

namespace ExoAuth.Application.Features.Email.Announcements.Commands.SendEmailAnnouncement;

/// <summary>
/// Command to send an email announcement to all recipients.
/// </summary>
public sealed record SendEmailAnnouncementCommand(
    Guid Id
) : ICommand<EmailAnnouncementDto>;
