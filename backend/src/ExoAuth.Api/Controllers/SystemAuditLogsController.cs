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
    [HttpGet]
    [SystemPermission(SystemPermissions.AuditRead)]
    [ProducesResponseType(typeof(CursorPagedList<SystemAuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20,
        [FromQuery] string? sort = null,
        [FromQuery] string? action = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        CancellationToken ct = default)
    {
        var query = new GetSystemAuditLogsQuery(
            cursor,
            limit,
            sort,
            action,
            userId,
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
