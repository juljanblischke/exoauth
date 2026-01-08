using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Email.Models;
using Mediator;

namespace ExoAuth.Application.Features.Email.Dlq.Queries.GetDlqEmails;

/// <summary>
/// Query to get emails in the dead letter queue.
/// </summary>
public sealed record GetDlqEmailsQuery(
    string? Cursor = null,
    int Limit = 20,
    string? Search = null,
    string Sort = "movedToDlqAt:desc"
) : IQuery<CursorPagedList<EmailLogDto>>;
