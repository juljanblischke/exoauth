using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Announcements.Commands.DeleteEmailAnnouncement;

public sealed class DeleteEmailAnnouncementHandler(
    IAppDbContext dbContext
) : ICommandHandler<DeleteEmailAnnouncementCommand, Unit>
{
    public async ValueTask<Unit> Handle(
        DeleteEmailAnnouncementCommand request,
        CancellationToken cancellationToken)
    {
        var announcement = await dbContext.EmailAnnouncements
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (announcement is null)
        {
            throw new EmailAnnouncementNotFoundException(request.Id);
        }

        if (!announcement.CanBeDeleted)
        {
            throw new EmailAnnouncementAlreadySentException(request.Id);
        }

        dbContext.EmailAnnouncements.Remove(announcement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
