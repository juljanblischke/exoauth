using ExoAuth.Api.Filters;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemAuditLogs.Models;
using ExoAuth.Application.Features.SystemAuditLogs.Queries.GetAuditLogFilters;
using ExoAuth.Application.Features.SystemAuditLogs.Queries.GetSystemAuditLogs;
using ExoAuth.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExoAuth.Api.Controllers;

[Route("api/system/audit-logs")]
[Authorize]
[RateLimit]
public sealed class SystemAuditLogsController : ApiControllerBase
{
    /// <summary>
    /// Get paginated list of system audit logs with filtering.
    /// </summary>
    /// <param name="search">Search in actor and target user email/name</param>
    /// <param name="actions">Comma-separated list of actions to filter by</param>
    /// <param name="involvedUserIds">Comma-separated list of user IDs (matches actor OR target)</param>
    [HttpGet]
    [SystemPermission(SystemPermissions.AuditRead)]
    [ProducesResponseType(typeof(CursorPagedList<SystemAuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20,
        [FromQuery] string? sort = null,
        [FromQuery] string? search = null,
        [FromQuery] string? actions = null,
        [FromQuery] string? involvedUserIds = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        CancellationToken ct = default)
    {
        // Parse comma-separated actions
        List<string>? actionsList = null;
        if (!string.IsNullOrWhiteSpace(actions))
        {
            actionsList = actions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        // Parse comma-separated user IDs
        List<Guid>? involvedUserIdsList = null;
        if (!string.IsNullOrWhiteSpace(involvedUserIds))
        {
            involvedUserIdsList = involvedUserIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => Guid.TryParse(id, out var guid) ? guid : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();
        }

        var query = new GetSystemAuditLogsQuery(
            cursor,
            limit,
            sort,
            search,
            actionsList,
            involvedUserIdsList,
            from,
            to,
            entityType,
            entityId
        );

        var result = await Mediator.Send(query, ct);

        return ApiOk(result.Items, result.Pagination);
    }

    /// <summary>
    /// Get available filter options for audit logs.
    /// </summary>
    [HttpGet("filters")]
    [SystemPermission(SystemPermissions.AuditRead)]
    [ProducesResponseType(typeof(AuditLogFiltersDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFilters(CancellationToken ct)
    {
        var query = new GetAuditLogFiltersQuery();

        var result = await Mediator.Send(query, ct);

        return ApiOk(result);
    }
}
