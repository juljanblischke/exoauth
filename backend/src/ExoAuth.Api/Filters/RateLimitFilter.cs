using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace ExoAuth.Api.Filters;

/// <summary>
/// Rate limit filter implementing sliding window algorithm with IP whitelist/blacklist support.
/// </summary>
public sealed class RateLimitFilter : IAsyncActionFilter
{
    private readonly IRateLimitService _rateLimitService;
    private readonly IIpRestrictionService _ipRestrictionService;
    private readonly string _presetName;
    private readonly ILogger<RateLimitFilter> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RateLimitFilter(
        IRateLimitService rateLimitService,
        IIpRestrictionService ipRestrictionService,
        ILogger<RateLimitFilter> logger,
        string presetName)
    {
        _rateLimitService = rateLimitService;
        _ipRestrictionService = ipRestrictionService;
        _logger = logger;
        _presetName = presetName;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var ipAddress = GetClientIpAddress(context.HttpContext);

        // Check IP restrictions (whitelist/blacklist)
        var ipCheck = await _ipRestrictionService.CheckIpAsync(ipAddress);

        if (ipCheck.IsBlacklisted)
        {
            _logger.LogWarning("Request blocked - IP {IpAddress} is blacklisted: {Reason}", ipAddress, ipCheck.Reason);

            var response = ApiResponse<object>.Error(
                "Access denied.",
                StatusCodes.Status403Forbidden,
                ApiError.Create(ErrorCodes.IpBlacklisted, ipCheck.Reason ?? "Your IP address has been blocked."));

            context.Result = new ContentResult
            {
                Content = JsonSerializer.Serialize(response, JsonOptions),
                ContentType = "application/json",
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        // Whitelisted IPs bypass rate limiting
        if (ipCheck.IsWhitelisted)
        {
            _logger.LogDebug("IP {IpAddress} is whitelisted, bypassing rate limit", ipAddress);
            await next();
            return;
        }

        // Get user ID for per-user limiting (if authenticated)
        Guid? userId = null;
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.HttpContext.User.FindFirst("sub")?.Value
                              ?? context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
            }
        }

        // Check rate limit
        var result = await _rateLimitService.CheckRateLimitAsync(_presetName, ipAddress, userId);

        // Set rate limit headers
        context.HttpContext.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
        context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
        context.HttpContext.Response.Headers["X-RateLimit-Reset"] = result.ResetAt.ToString();

        if (!result.IsAllowed)
        {
            context.HttpContext.Response.Headers["Retry-After"] = result.RetryAfterSeconds.ToString();

            // Record violation for potential auto-blacklist
            var shouldBlacklist = await _rateLimitService.RecordViolationAsync(ipAddress);
            if (shouldBlacklist)
            {
                var preset = _rateLimitService.GetPreset("default");
                var blockDuration = 60; // Default to 60 minutes if settings not available
                await _ipRestrictionService.AutoBlacklistAsync(
                    ipAddress,
                    $"Repeated rate limit violations on {_presetName} endpoint",
                    blockDuration);
            }

            var response = ApiResponse<object>.Error(
                "Rate limit exceeded. Please try again later.",
                StatusCodes.Status429TooManyRequests,
                ApiError.Create(ErrorCodes.RateLimitExceeded, $"Too many requests. Please wait {result.RetryAfterSeconds} seconds before trying again."));

            context.Result = new ContentResult
            {
                Content = JsonSerializer.Serialize(response, JsonOptions),
                ContentType = "application/json",
                StatusCode = StatusCodes.Status429TooManyRequests
            };

            return;
        }

        await next();
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP (when behind a proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
