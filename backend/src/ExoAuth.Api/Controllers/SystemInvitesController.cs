using ExoAuth.Api.Filters;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemInvites.Commands.ResendInvite;
using ExoAuth.Application.Features.SystemInvites.Commands.RevokeInvite;
using ExoAuth.Application.Features.SystemInvites.Models;
using ExoAuth.Application.Features.SystemInvites.Queries.GetSystemInvite;
using ExoAuth.Application.Features.SystemInvites.Queries.GetSystemInvites;
using ExoAuth.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExoAuth.Api.Controllers;

[Route("api/system/invites")]
[Authorize]
[RateLimit]
public sealed class SystemInvitesController : ApiControllerBase
{
    /// <summary>
    /// Get paginated list of system invites.
    /// </summary>
    [HttpGet]
    [SystemPermission(SystemPermissions.UsersRead)]
    [ProducesResponseType(typeof(CursorPagedList<SystemInviteListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        // Parse comma-separated statuses
        List<string>? statuses = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            statuses = status
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        var query = new GetSystemInvitesQuery(cursor, limit, search, statuses);

        var result = await Mediator.Send(query, ct);

        return ApiOk(result.Items, result.Pagination);
    }

    /// <summary>
    /// Get a system invite by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [SystemPermission(SystemPermissions.UsersRead)]
    [ProducesResponseType(typeof(SystemInviteDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetSystemInviteQuery(id);

        var result = await Mediator.Send(query, ct);

        if (result is null)
        {
            return ApiNotFound("Invitation not found");
        }

        return ApiOk(result);
    }

    /// <summary>
    /// Revoke a system invite.
    /// </summary>
    [HttpPost("{id:guid}/revoke")]
    [SystemPermission(SystemPermissions.UsersUpdate)]
    [ProducesResponseType(typeof(SystemInviteListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        var command = new RevokeInviteCommand(id);

        var result = await Mediator.Send(command, ct);

        return ApiOk(result);
    }

    /// <summary>
    /// Resend a system invite email.
    /// </summary>
    [HttpPost("{id:guid}/resend")]
    [SystemPermission(SystemPermissions.UsersUpdate)]
    [ProducesResponseType(typeof(SystemInviteListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Resend(Guid id, CancellationToken ct)
    {
        var command = new ResendInviteCommand(id);

        var result = await Mediator.Send(command, ct);

        return ApiOk(result);
    }
}
