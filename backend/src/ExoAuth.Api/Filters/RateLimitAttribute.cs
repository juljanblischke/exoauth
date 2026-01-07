using Microsoft.AspNetCore.Mvc;

namespace ExoAuth.Api.Filters;

/// <summary>
/// Applies rate limiting to the decorated endpoint using named presets.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RateLimitAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Applies rate limiting using the specified preset name.
    /// Available presets: login, register, forgot-password, mfa, sensitive, default, relaxed
    /// </summary>
    /// <param name="presetName">Name of the rate limit preset to use.</param>
    public RateLimitAttribute(string presetName = "default") : base(typeof(RateLimitFilter))
    {
        Arguments = new object[] { presetName };
    }
}
