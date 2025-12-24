using Microsoft.AspNetCore.Mvc;

namespace ExoAuth.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RateLimitAttribute : TypeFilterAttribute
{
    public RateLimitAttribute(int requestsPerMinute = 100) : base(typeof(RateLimitFilter))
    {
        Arguments = new object[] { requestsPerMinute };
    }
}
