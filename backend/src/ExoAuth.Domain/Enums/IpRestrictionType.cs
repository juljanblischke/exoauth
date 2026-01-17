namespace ExoAuth.Domain.Enums;

/// <summary>
/// Represents the type of IP restriction.
/// </summary>
public enum IpRestrictionType
{
    /// <summary>
    /// IP address is whitelisted (bypasses rate limiting).
    /// </summary>
    Whitelist = 0,

    /// <summary>
    /// IP address is blacklisted (blocked from all requests).
    /// </summary>
    Blacklist = 1
}
