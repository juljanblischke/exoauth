namespace ExoAuth.Domain.Enums;

/// <summary>
/// Represents the status of a device in the device trust system.
/// </summary>
public enum DeviceStatus
{
    /// <summary>
    /// Device is pending approval from the user.
    /// </summary>
    PendingApproval = 0,

    /// <summary>
    /// Device has been approved and is trusted.
    /// </summary>
    Trusted = 1,

    /// <summary>
    /// Device has been revoked and is no longer trusted.
    /// </summary>
    Revoked = 2
}
