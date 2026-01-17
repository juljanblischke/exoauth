using ExoAuth.Api.Filters;
using ExoAuth.Application.Features.SystemPermissions.Models;
using ExoAuth.Application.Features.SystemPermissions.Queries.GetSystemPermissions;
using ExoAuth.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExoAuth.Api.Controllers;

[Route("api/system/permissions")]
[Authorize]
[RateLimit]
public sealed class SystemPermissionsController : ApiControllerBase
{
    /// <summary>
    /// Get all available system permissions.
    /// </summary>
    [HttpGet]
    [SystemPermission(SystemPermissions.UsersRead)]
    [ProducesResponseType(typeof(IReadOnlyList<SystemPermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool groupByCategory = false,
        CancellationToken ct = default)
    {
        var query = new GetSystemPermissionsQuery(groupByCategory);

        var result = await Mediator.Send(query, ct);

        return ApiOk(result);
    }
}
