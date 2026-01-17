using Mediator;

namespace ExoAuth.Application.Features.Email.Announcements.Commands.DeleteEmailAnnouncement;

/// <summary>
/// Command to delete an email announcement.
/// </summary>
public sealed record DeleteEmailAnnouncementCommand(
    Guid Id
) : ICommand<Unit>;
