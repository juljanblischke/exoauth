using ExoAuth.Api.Filters;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.IpRestrictions.Commands.CreateIpRestriction;
using ExoAuth.Application.Features.IpRestrictions.Commands.DeleteIpRestriction;
using ExoAuth.Application.Features.IpRestrictions.Commands.UpdateIpRestriction;
using ExoAuth.Application.Features.IpRestrictions.Models;
using ExoAuth.Application.Features.IpRestrictions.Queries.GetIpRestrictions;
using ExoAuth.Domain.Constants;
using ExoAuth.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExoAuth.Api.Controllers;

/// <summary>
/// Controller for managing IP restrictions (whitelist/blacklist).
/// </summary>
[Route("api/system/ip-restrictions")]
[Authorize]
[RateLimit]
public sealed class SystemIpRestrictionsController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    public SystemIpRestrictionsController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get paginated list of IP restrictions.
    /// </summary>
    /// <param name="cursor">Pagination cursor.</param>
    /// <param name="limit">Number of items per page (default 20).</param>
    /// <param name="type">Filter by type (whitelist/blacklist).</param>
    /// <param name="source">Filter by source (manual/auto).</param>
    /// <param name="includeExpired">Include expired restrictions (default false).</param>
    /// <param name="search">Search in IP address or reason.</param>
    /// <param name="sort">Sort field and direction (e.g., "createdAt:desc").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of IP restrictions.</returns>
    [HttpGet]
    [SystemPermission(SystemPermissions.IpRestrictionsRead)]
    [ProducesResponseType(typeof(ApiResponse<CursorPagedList<IpRestrictionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20,
        [FromQuery] IpRestrictionType? type = null,
        [FromQuery] IpRestrictionSource? source = null,
        [FromQuery] bool includeExpired = false,
        [FromQuery] string? search = null,
        [FromQuery] string sort = "createdAt:desc",
        CancellationToken ct = default)
    {
        var query = new GetIpRestrictionsQuery(
            cursor,
            limit,
            type,
            source,
            includeExpired,
            search,
            sort);

        var result = await Mediator.Send(query, ct);

        return ApiOk(result.Items, result.Pagination);
    }

    /// <summary>
    /// Create a new IP restriction (whitelist or blacklist entry).
    /// </summary>
    /// <param name="request">The IP restriction details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created IP restriction.</returns>
    [HttpPost]
    [SystemPermission(SystemPermissions.IpRestrictionsManage)]
    [ProducesResponseType(typeof(ApiResponse<IpRestrictionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateIpRestrictionRequest request,
        CancellationToken ct)
    {
        var command = new CreateIpRestrictionCommand(
            request.IpAddress,
            request.Type,
            request.Reason,
            request.ExpiresAt,
            _currentUserService.UserId!.Value);

        var result = await Mediator.Send(command, ct);

        return ApiCreated(result);
    }

    /// <summary>
    /// Update an existing IP restriction.
    /// </summary>
    /// <param name="id">The ID of the IP restriction to update.</param>
    /// <param name="request">The updated IP restriction details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated IP restriction.</returns>
    [HttpPatch("{id:guid}")]
    [SystemPermission(SystemPermissions.IpRestrictionsManage)]
    [ProducesResponseType(typeof(ApiResponse<IpRestrictionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateIpRestrictionRequest request,
        CancellationToken ct)
    {
        var command = new UpdateIpRestrictionCommand(
            id,
            request.Type,
            request.Reason,
            request.ExpiresAt,
            _currentUserService.UserId!.Value);

        var result = await Mediator.Send(command, ct);

        return ApiOk(result);
    }

    /// <summary>
    /// Delete an IP restriction.
    /// </summary>
    /// <param name="id">The ID of the IP restriction to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [SystemPermission(SystemPermissions.IpRestrictionsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var command = new DeleteIpRestrictionCommand(id);

        await Mediator.Send(command, ct);

        return ApiNoContent();
    }
}
