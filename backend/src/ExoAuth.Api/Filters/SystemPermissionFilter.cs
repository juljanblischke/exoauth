using System.Security.Claims;
using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ExoAuth.Api.Filters;

public sealed class SystemPermissionFilter : IAsyncAuthorizationFilter
{
    private readonly IPermissionCacheService _permissionCache;
    private readonly string[] _requiredPermissions;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SystemPermissionFilter(IPermissionCacheService permissionCache, string[] permissions)
    {
        _permissionCache = permissionCache;
        _requiredPermissions = permissions;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            SetUnauthorizedResult(context, "Authentication required");
            return;
        }

        // Get user ID from claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
            ?? user.FindFirst("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            SetUnauthorizedResult(context, "Invalid token");
            return;
        }

        // Check user type - must be "system" for system permissions
        var userType = user.FindFirst("type")?.Value;
        if (userType != "system")
        {
            SetForbiddenResult(context, "System-level access required");
            return;
        }

        // Get user permissions from cache or JWT
        var permissions = await GetUserPermissionsAsync(context, userId);

        // Check if user has at least one of the required permissions
        var hasPermission = _requiredPermissions.Any(required =>
            permissions.Contains(required, StringComparer.OrdinalIgnoreCase));

        if (!hasPermission)
        {
            SetForbiddenResult(context, "Insufficient permissions");
            return;
        }
    }

    private async Task<IReadOnlyList<string>> GetUserPermissionsAsync(
        AuthorizationFilterContext context,
        Guid userId)
    {
        // First try to get from cache
        try
        {
            var cachedPermissions = await _permissionCache.GetPermissionsAsync(userId);
            if (cachedPermissions is not null && cachedPermissions.Count > 0)
            {
                return cachedPermissions;
            }
        }
        catch
        {
            // Redis unavailable, fall through to JWT claims
        }

        // Fallback: get from JWT claims
        var permissionClaims = context.HttpContext.User
            .FindAll("permission")
            .Select(c => c.Value)
            .ToList();

        return permissionClaims;
    }

    private static void SetUnauthorizedResult(AuthorizationFilterContext context, string message)
    {
        var response = ApiResponse<object>.Error(
            message,
            StatusCodes.Status401Unauthorized,
            ApiError.Create(ErrorCodes.AuthUnauthorized, message));

        context.Result = new ContentResult
        {
            Content = JsonSerializer.Serialize(response, JsonOptions),
            ContentType = "application/json",
            StatusCode = StatusCodes.Status401Unauthorized
        };
    }

    private static void SetForbiddenResult(AuthorizationFilterContext context, string message)
    {
        var response = ApiResponse<object>.Error(
            message,
            StatusCodes.Status403Forbidden,
            ApiError.Create(ErrorCodes.AuthForbidden, message));

        context.Result = new ContentResult
        {
            Content = JsonSerializer.Serialize(response, JsonOptions),
            ContentType = "application/json",
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
