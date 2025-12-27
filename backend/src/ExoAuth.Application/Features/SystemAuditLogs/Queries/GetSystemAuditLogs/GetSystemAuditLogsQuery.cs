using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemAuditLogs.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemAuditLogs.Queries.GetSystemAuditLogs;

/// <summary>
/// Query to get paginated list of system audit logs with filtering.
/// </summary>
public sealed record GetSystemAuditLogsQuery(
    string? Cursor = null,
    int Limit = 20,
    string? Sort = null,
    string? Search = null,
    List<string>? Actions = null,
    List<Guid>? InvolvedUserIds = null,
    DateTime? From = null,
    DateTime? To = null,
    string? EntityType = null,
    Guid? EntityId = null
) : IQuery<CursorPagedList<SystemAuditLogDto>>;
