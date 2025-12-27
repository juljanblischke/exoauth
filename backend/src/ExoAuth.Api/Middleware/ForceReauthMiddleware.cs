using System.Security.Claims;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;

namespace ExoAuth.Api.Middleware;

/// <summary>
/// Middleware that checks if a user has been flagged for forced re-authentication.
/// Returns 401 Unauthorized if the flag is set.
/// </summary>
public sealed class ForceReauthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ForceReauthMiddleware> _logger;

    public ForceReauthMiddleware(RequestDelegate next, ILogger<ForceReauthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IForceReauthService forceReauthService, IAuditService auditService)
    {
        // Only check authenticated requests
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Skip for login/refresh endpoints (allow re-authentication)
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (path.Contains("/api/auth/login") ||
            path.Contains("/api/auth/refresh") ||
            path.Contains("/api/auth/register") ||
            path.Contains("/api/auth/accept-invite") ||
            path.Contains("/api/auth/invite"))
        {
            await _next(context);
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            await _next(context);
            return;
        }

        // Check if user has force re-auth flag
        if (await forceReauthService.HasFlagAsync(userId, context.RequestAborted))
        {
            _logger.LogWarning("Force re-auth triggered for user {UserId} on {Path}", userId, path);

            // Audit log
            await auditService.LogWithContextAsync(
                AuditActions.ForceReauthTriggered,
                userId,
                null,
                null,
                null,
                new { Endpoint = path },
                context.RequestAborted
            );

            // Return 401 with specific error
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = "error",
                statusCode = 401,
                message = "Session invalidated due to permission changes. Please login again.",
                errors = new[]
                {
                    new { field = (string?)null, code = ErrorCodes.AuthForceReauth, message = "Re-authentication required" }
                }
            };

            await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
            return;
        }

        await _next(context);
    }
}
