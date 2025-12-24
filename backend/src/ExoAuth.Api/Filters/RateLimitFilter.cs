using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace ExoAuth.Api.Filters;

public sealed class RateLimitFilter : IAsyncActionFilter
{
    private readonly ICacheService _cacheService;
    private readonly int _requestsPerMinute;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RateLimitFilter(ICacheService cacheService, int requestsPerMinute)
    {
        _cacheService = cacheService;
        _requestsPerMinute = requestsPerMinute;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var ipAddress = GetClientIpAddress(context.HttpContext);
        var endpoint = context.HttpContext.Request.Path.ToString().ToLowerInvariant();
        var key = $"rate_limit:{ipAddress}:{endpoint}";

        var currentCount = await _cacheService.IncrementAsync(key, 1, TimeSpan.FromMinutes(1));

        // Set rate limit headers
        context.HttpContext.Response.Headers["X-RateLimit-Limit"] = _requestsPerMinute.ToString();
        context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, _requestsPerMinute - currentCount).ToString();

        if (currentCount > _requestsPerMinute)
        {
            context.HttpContext.Response.Headers["Retry-After"] = "60";

            var response = ApiResponse<object>.Error(
                "Rate limit exceeded. Please try again later.",
                StatusCodes.Status429TooManyRequests,
                ApiError.Create(ErrorCodes.RateLimitExceeded, "Too many requests. Please wait before trying again."));

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
