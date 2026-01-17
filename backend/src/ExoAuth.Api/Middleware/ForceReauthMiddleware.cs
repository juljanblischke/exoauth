using System.Security.Claims;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;

namespace ExoAuth.Api.Middleware;

/// <summary>
/// Middleware that checks if a user has been flagged for forced re-authentication
/// or if their session has been revoked. Returns 401 Unauthorized if either condition is met.
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

    public async Task InvokeAsync(
        HttpContext context,
        IForceReauthService forceReauthService,
        IRevokedSessionService revokedSessionService,
        IAuditService auditService)
    {
        // Only check authenticated requests
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Skip for auth endpoints that need to work during force-reauth
        // and public config endpoints that may receive tokens from logged-in users
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (path.Contains("/api/auth/login") ||
            path.Contains("/api/auth/register") ||
            path.Contains("/api/auth/accept-invite") ||
            path.Contains("/api/auth/invite") ||
            path.Contains("/api/auth/logout") ||
            path.Contains("/api/auth/mfa/verify") ||
            path.Contains("/api/auth/mfa/setup") ||
            path.Contains("/api/auth/mfa/confirm") ||
            path.Contains("/api/auth/forgot-password") ||
            path.Contains("/api/auth/reset-password") ||
            path.Contains("/api/auth/passkeys/login") ||
            path.Contains("/api/captcha/config"))
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

        // Get session ID from claims
        var sessionIdClaim = context.User.FindFirst("session_id")?.Value;
        if (!Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            await _next(context);
            return;
        }

        // Check if session has been revoked
        if (await revokedSessionService.IsSessionRevokedAsync(sessionId, context.RequestAborted))
        {
            _logger.LogWarning("Revoked session {SessionId} attempted access for user {UserId} on {Path}", sessionId, userId, path);

            await ReturnUnauthorizedResponse(
                context,
                "Session has been revoked. Please login again.",
                ErrorCodes.SessionRevoked);
            return;
        }

        // Check if session has force re-auth flag (session-based, not user-based)
        if (await forceReauthService.HasFlagAsync(sessionId, context.RequestAborted))
        {
            _logger.LogWarning("Force re-auth triggered for session {SessionId} (user {UserId}) on {Path}", sessionId, userId, path);

            // Audit log
            await auditService.LogWithContextAsync(
                AuditActions.ForceReauthTriggered,
                userId,
                null,
                "DeviceSession",
                sessionId,
                new { Endpoint = path, SessionId = sessionId },
                context.RequestAborted
            );

            await ReturnUnauthorizedResponse(
                context,
                "Session invalidated due to permission changes. Please login again.",
                ErrorCodes.AuthForceReauth);
            return;
        }

        await _next(context);
    }

    private static async Task ReturnUnauthorizedResponse(HttpContext context, string message, string errorCode)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = "error",
            statusCode = 401,
            message,
            errors = new[]
            {
                new { field = (string?)null, code = errorCode, message = "Re-authentication required" }
            }
        };

        await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }
}
