using ExoAuth.Application.Common.Models;
using Mediator;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Enums;

namespace ExoAuth.Application.Features.Email.Announcements.Queries.GetEmailAnnouncements;

/// <summary>
/// Query to get paginated email announcements.
/// </summary>
public sealed record GetEmailAnnouncementsQuery(
    string? Cursor = null,
    int Limit = 20,
    EmailAnnouncementStatus? Status = null,
    string? Search = null,
    string Sort = "createdAt:desc"
) : IQuery<CursorPagedList<EmailAnnouncementDto>>;
