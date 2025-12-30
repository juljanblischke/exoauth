using ExoAuth.Api.Filters;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Application.Features.SystemUsers.Commands.AnonymizeUser;
using ExoAuth.Application.Features.SystemUsers.Commands.ActivateSystemUser;
using ExoAuth.Application.Features.SystemUsers.Commands.DeactivateSystemUser;
using ExoAuth.Application.Features.SystemUsers.Commands.InviteSystemUser;
using ExoAuth.Application.Features.SystemUsers.Commands.ResetUserMfa;
using ExoAuth.Application.Features.SystemUsers.Commands.RevokeUserSessions;
using ExoAuth.Application.Features.SystemUsers.Commands.UnlockUser;
using ExoAuth.Application.Features.SystemUsers.Commands.UpdatePermissions;
using ExoAuth.Application.Features.SystemUsers.Commands.UpdateSystemUser;
using ExoAuth.Application.Features.SystemUsers.Models;
using ExoAuth.Application.Features.SystemUsers.Queries.GetSystemUser;
using ExoAuth.Application.Features.SystemUsers.Queries.GetSystemUsers;
using ExoAuth.Application.Features.SystemUsers.Queries.GetUserSessions;
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
    /// <param name="cursor">Pagination cursor.</param>
    /// <param name="limit">Number of items per page (default: 20).</param>
    /// <param name="sort">Sort field and direction (e.g., "email:asc").</param>
    /// <param name="search">Search term for email, firstName, lastName.</param>
    /// <param name="permissionIds">Comma-separated permission IDs to filter by.</param>
    /// <param name="isActive">Filter by active status.</param>
    /// <param name="isAnonymized">Filter by anonymized status. Default: false (hide anonymized users).</param>
    /// <param name="isLocked">Filter by locked status.</param>
    /// <param name="mfaEnabled">Filter by MFA enabled status.</param>
    /// <param name="ct">Cancellation token.</param>
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
        [FromQuery] string? permissionIds = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isAnonymized = false,
        [FromQuery] bool? isLocked = null,
        [FromQuery] bool? mfaEnabled = null,
        CancellationToken ct = default)
    {
        // Parse comma-separated permission IDs
        List<Guid>? parsedPermissionIds = null;
        if (!string.IsNullOrWhiteSpace(permissionIds))
        {
            parsedPermissionIds = permissionIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => Guid.TryParse(id, out var guid) ? guid : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();
        }

        var query = new GetSystemUsersQuery(
            cursor,
            limit,
            sort,
            search,
            parsedPermissionIds,
            isActive,
            isAnonymized,
            isLocked,
            mfaEnabled
        );

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
    /// Deactivate a system user.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [SystemPermission(SystemPermissions.UsersDeactivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var command = new DeactivateSystemUserCommand(id);

        await Mediator.Send(command, ct);

        return ApiNoContent();
    }

    /// <summary>
    /// Activate a previously deactivated system user.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [SystemPermission(SystemPermissions.UsersActivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var command = new ActivateSystemUserCommand(id);

        await Mediator.Send(command, ct);

        return ApiNoContent();
    }

    #region Admin Security Actions

    /// <summary>
    /// Reset MFA for a system user (admin action).
    /// </summary>
    [HttpPost("{id:guid}/mfa/reset")]
    [SystemPermission(SystemPermissions.UsersMfaReset)]
    [ProducesResponseType(typeof(ResetUserMfaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetMfa(Guid id, ResetUserMfaRequest? request, CancellationToken ct)
    {
        var command = new ResetUserMfaCommand(id, request?.Reason);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Unlock a locked system user account.
    /// </summary>
    [HttpPost("{id:guid}/unlock")]
    [SystemPermission(SystemPermissions.UsersUnlock)]
    [ProducesResponseType(typeof(UnlockUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlock(Guid id, UnlockUserRequest? request, CancellationToken ct)
    {
        var command = new UnlockUserCommand(id, request?.Reason);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Get all sessions for a system user.
    /// </summary>
    [HttpGet("{id:guid}/sessions")]
    [SystemPermission(SystemPermissions.UsersSessionsView)]
    [ProducesResponseType(typeof(List<DeviceSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessions(Guid id, CancellationToken ct)
    {
        var query = new GetUserSessionsQuery(id);
        var result = await Mediator.Send(query, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Revoke all sessions for a system user.
    /// </summary>
    [HttpDelete("{id:guid}/sessions")]
    [SystemPermission(SystemPermissions.UsersSessionsRevoke)]
    [ProducesResponseType(typeof(RevokeUserSessionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSessions(Guid id, CancellationToken ct)
    {
        var command = new RevokeUserSessionsCommand(id);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Anonymize a system user (GDPR deletion).
    /// </summary>
    [HttpPost("{id:guid}/anonymize")]
    [SystemPermission(SystemPermissions.UsersAnonymize)]
    [ProducesResponseType(typeof(AnonymizeUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Anonymize(Guid id, CancellationToken ct)
    {
        var command = new AnonymizeUserCommand(id);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    #endregion
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

public sealed record ResetUserMfaRequest(
    string? Reason = null
);

public sealed record UnlockUserRequest(
    string? Reason = null
);
