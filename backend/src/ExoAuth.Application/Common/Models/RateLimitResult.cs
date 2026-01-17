namespace ExoAuth.Application.Common.Models;

/// <summary>
/// Result of a rate limit check.
/// </summary>
public sealed record RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed.
    /// </summary>
    public bool IsAllowed { get; init; }

    /// <summary>
    /// The maximum number of requests allowed in the current window.
    /// </summary>
    public int Limit { get; init; }

    /// <summary>
    /// Number of requests remaining in the current window.
    /// </summary>
    public int Remaining { get; init; }

    /// <summary>
    /// Unix timestamp when the rate limit resets.
    /// </summary>
    public long ResetAt { get; init; }

    /// <summary>
    /// Number of seconds until the rate limit resets.
    /// </summary>
    public int RetryAfterSeconds { get; init; }

    /// <summary>
    /// Which window was exceeded (if any): "minute", "hour", or null if allowed.
    /// </summary>
    public string? ExceededWindow { get; init; }

    /// <summary>
    /// Creates an allowed result.
    /// </summary>
    public static RateLimitResult Allowed(int limit, int remaining, long resetAt) => new()
    {
        IsAllowed = true,
        Limit = limit,
        Remaining = remaining,
        ResetAt = resetAt,
        RetryAfterSeconds = 0,
        ExceededWindow = null
    };

    /// <summary>
    /// Creates an exceeded result.
    /// </summary>
    public static RateLimitResult Exceeded(int limit, int remaining, long resetAt, int retryAfterSeconds, string window) => new()
    {
        IsAllowed = false,
        Limit = limit,
        Remaining = remaining,
        ResetAt = resetAt,
        RetryAfterSeconds = retryAfterSeconds,
        ExceededWindow = window
    };
}
