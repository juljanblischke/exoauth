using ExoAuth.Api.Filters;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemUsers.Commands.DeleteSystemUser;
using ExoAuth.Application.Features.SystemUsers.Commands.InviteSystemUser;
using ExoAuth.Application.Features.SystemUsers.Commands.UpdatePermissions;
using ExoAuth.Application.Features.SystemUsers.Commands.UpdateSystemUser;
using ExoAuth.Application.Features.SystemUsers.Models;
using ExoAuth.Application.Features.SystemUsers.Queries.GetSystemUser;
using ExoAuth.Application.Features.SystemUsers.Queries.GetSystemUsers;
using ExoAuth.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExoAuth.Api.Controllers;

[Route("api/system/users")]
[Authorize]
[RateLimit]
public sealed class SystemUsersController : ApiControllerBase
{
    /// <summary>
    /// Get paginated list of system users.
    /// </summary>
    [HttpGet]
    [SystemPermission(SystemPermissions.UsersRead)]
    [ProducesResponseType(typeof(CursorPagedList<SystemUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        [FromQuery] string? sort = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = new GetSystemUsersQuery(cursor, limit, sort, search);

        var result = await Mediator.Send(query, ct);

        return ApiOk(result.Items, result.Pagination);
    }

    /// <summary>
    /// Get a system user by ID with their permissions.
    /// </summary>
    [HttpGet("{id:guid}")]
    [SystemPermission(SystemPermissions.UsersRead)]
    [ProducesResponseType(typeof(SystemUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetSystemUserQuery(id);

        var result = await Mediator.Send(query, ct);

        if (result is null)
        {
            return ApiNotFound("System user not found");
        }

        return ApiOk(result);
    }

    /// <summary>
    /// Invite a new system user.
    /// </summary>
    [HttpPost("invite")]
    [SystemPermission(SystemPermissions.UsersCreate)]
    [ProducesResponseType(typeof(SystemInviteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Invite(InviteSystemUserRequest request, CancellationToken ct)
    {
        var command = new InviteSystemUserCommand(
            request.Email,
            request.FirstName,
            request.LastName,
            request.PermissionIds
        );

        var result = await Mediator.Send(command, ct);

        return ApiCreated(result);
    }

    /// <summary>
    /// Update a system user's profile.
    /// </summary>
    [HttpPut("{id:guid}")]
    [SystemPermission(SystemPermissions.UsersUpdate)]
    [ProducesResponseType(typeof(SystemUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateSystemUserRequest request, CancellationToken ct)
    {
        var command = new UpdateSystemUserCommand(
            id,
            request.FirstName,
            request.LastName,
            request.IsActive
        );

        var result = await Mediator.Send(command, ct);

        return ApiOk(result);
    }

    /// <summary>
    /// Update a system user's permissions (replaces all).
    /// </summary>
    [HttpPut("{id:guid}/permissions")]
    [SystemPermission(SystemPermissions.UsersUpdate)]
    [ProducesResponseType(typeof(SystemUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePermissions(Guid id, UpdatePermissionsRequest request, CancellationToken ct)
    {
        var command = new UpdateSystemUserPermissionsCommand(id, request.PermissionIds);

        var result = await Mediator.Send(command, ct);

        return ApiOk(result);
    }

    /// <summary>
    /// Delete (deactivate) a system user.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [SystemPermission(SystemPermissions.UsersDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var command = new DeleteSystemUserCommand(id);

        await Mediator.Send(command, ct);

        return ApiNoContent();
    }
}

// Request DTOs
public sealed record InviteSystemUserRequest(
    string Email,
    string FirstName,
    string LastName,
    List<Guid> PermissionIds
);

public sealed record UpdateSystemUserRequest(
    string? FirstName = null,
    string? LastName = null,
    bool? IsActive = null
);

public sealed record UpdatePermissionsRequest(
    List<Guid> PermissionIds
);
