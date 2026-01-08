using ExoAuth.Application.Features.Email.Models;
using Mediator;

namespace ExoAuth.Application.Features.Email.Announcements.Queries.GetEmailAnnouncement;

/// <summary>
/// Query to get a specific email announcement by ID.
/// </summary>
public sealed record GetEmailAnnouncementQuery(
    Guid Id
) : IQuery<EmailAnnouncementDetailDto>;
