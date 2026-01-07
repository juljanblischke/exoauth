using ExoAuth.Domain.Enums;

namespace ExoAuth.Domain.Entities;

/// <summary>
/// Represents an IP address restriction (whitelist or blacklist).
/// Supports both individual IPs and CIDR notation.
/// </summary>
public sealed class IpRestriction : BaseEntity
{
    /// <summary>
    /// IP address or CIDR notation (e.g., "192.168.1.1" or "10.0.0.0/8").
    /// </summary>
    public string IpAddress { get; private set; } = string.Empty;

    /// <summary>
    /// Type of restriction (Whitelist or Blacklist).
    /// </summary>
    public IpRestrictionType Type { get; private set; }

    /// <summary>
    /// Reason for the restriction.
    /// </summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>
    /// Source of the restriction (Manual by admin or Auto by system).
    /// </summary>
    public IpRestrictionSource Source { get; private set; }

    /// <summary>
    /// When the restriction expires. Null means permanent.
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// User ID who created this restriction (null for auto-created).
    /// </summary>
    public Guid? CreatedByUserId { get; private set; }

    /// <summary>
    /// Navigation property to the user who created this restriction.
    /// </summary>
    public SystemUser? CreatedByUser { get; private set; }

    private IpRestriction() { } // EF Core

    /// <summary>
    /// Creates a new manual IP restriction.
    /// </summary>
    public static IpRestriction CreateManual(
        string ipAddress,
        IpRestrictionType type,
        string reason,
        DateTime? expiresAt,
        Guid createdByUserId)
    {
        return new IpRestriction
        {
            IpAddress = ipAddress,
            Type = type,
            Reason = reason,
            Source = IpRestrictionSource.Manual,
            ExpiresAt = expiresAt,
            CreatedByUserId = createdByUserId
        };
    }

    /// <summary>
    /// Creates a new automatic IP restriction (e.g., from repeated rate limit violations).
    /// </summary>
    public static IpRestriction CreateAuto(
        string ipAddress,
        IpRestrictionType type,
        string reason,
        DateTime expiresAt)
    {
        return new IpRestriction
        {
            IpAddress = ipAddress,
            Type = type,
            Reason = reason,
            Source = IpRestrictionSource.Auto,
            ExpiresAt = expiresAt,
            CreatedByUserId = null
        };
    }

    /// <summary>
    /// Checks if this restriction is currently active (not expired).
    /// </summary>
    public bool IsActive(DateTime utcNow)
    {
        return ExpiresAt == null || ExpiresAt > utcNow;
    }
}
