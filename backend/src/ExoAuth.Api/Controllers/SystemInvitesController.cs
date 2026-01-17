using ExoAuth.Api.Filters;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemInvites.Commands.ResendInvite;
using ExoAuth.Application.Features.SystemInvites.Commands.RevokeInvite;
using ExoAuth.Application.Features.SystemInvites.Commands.UpdateInvite;
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
    /// <param name="cursor">Pagination cursor</param>
    /// <param name="limit">Number of items per page (1-100, default: 20)</param>
    /// <param name="search">Search by email, firstName, or lastName</param>
    /// <param name="status">Comma-separated status filter (pending, accepted, revoked, expired)</param>
    /// <param name="sort">Sort field:direction (email:asc, firstName:desc, lastName:asc, createdAt:desc, expiresAt:asc)</param>
    /// <param name="includeExpired">Include expired invites (default: false)</param>
    /// <param name="includeRevoked">Include revoked invites (default: false)</param>
    /// <param name="ct">Cancellation token</param>
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
        [FromQuery] string sort = "createdAt:desc",
        [FromQuery] bool includeExpired = false,
        [FromQuery] bool includeRevoked = false,
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

        var query = new GetSystemInvitesQuery(cursor, limit, search, statuses, sort, includeExpired, includeRevoked);

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

    /// <summary>
    /// Update a pending system invite.
    /// </summary>
    /// <remarks>
    /// Only pending invites can be edited. Accepted, expired, or revoked invites cannot be modified.
    /// At least one field (firstName, lastName, or permissionIds) must be provided.
    /// </remarks>
    [HttpPatch("{id:guid}")]
    [SystemPermission(SystemPermissions.UsersUpdate)]
    [ProducesResponseType(typeof(SystemInviteListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateInviteRequest request,
        CancellationToken ct)
    {
        var command = new UpdateInviteCommand(
            id,
            request.FirstName,
            request.LastName,
            request.PermissionIds);

        var result = await Mediator.Send(command, ct);

        return ApiOk(result);
    }
}

/// <summary>
/// Request body for updating a system invite.
/// </summary>
public sealed record UpdateInviteRequest(
    string? FirstName,
    string? LastName,
    List<Guid>? PermissionIds
);
