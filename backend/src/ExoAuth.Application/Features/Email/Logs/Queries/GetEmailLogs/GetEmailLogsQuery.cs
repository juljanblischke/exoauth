using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Enums;
using Mediator;

namespace ExoAuth.Application.Features.Email.Logs.Queries.GetEmailLogs;

/// <summary>
/// Query to get paginated email logs with filtering.
/// </summary>
public sealed record GetEmailLogsQuery(
    string? Cursor = null,
    int Limit = 20,
    EmailStatus? Status = null,
    string? TemplateName = null,
    string? Search = null,
    Guid? RecipientUserId = null,
    Guid? AnnouncementId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string Sort = "createdAt:desc"
) : IQuery<CursorPagedList<EmailLogDto>>;
