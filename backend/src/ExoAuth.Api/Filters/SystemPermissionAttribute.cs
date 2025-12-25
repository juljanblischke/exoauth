using Microsoft.AspNetCore.Mvc;

namespace ExoAuth.Api.Filters;

/// <summary>
/// Requires the user to have the specified system permission(s) to access the endpoint.
/// Multiple permissions can be specified (user must have at least one).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class SystemPermissionAttribute : TypeFilterAttribute
{
    public SystemPermissionAttribute(params string[] permissions) : base(typeof(SystemPermissionFilter))
    {
        Arguments = new object[] { permissions };
    }
}
