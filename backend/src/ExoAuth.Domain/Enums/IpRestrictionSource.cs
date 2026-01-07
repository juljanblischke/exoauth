namespace ExoAuth.Domain.Enums;

/// <summary>
/// Represents the source of an IP restriction.
/// </summary>
public enum IpRestrictionSource
{
    /// <summary>
    /// Restriction was added manually by an administrator.
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Restriction was added automatically by the system (e.g., repeated rate limit violations).
    /// </summary>
    Auto = 1
}
