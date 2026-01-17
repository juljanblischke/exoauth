namespace ExoAuth.Domain.Enums;

/// <summary>
/// Represents the status of a device approval request.
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// Approval is pending user action.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Device has been approved by the user.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Device has been denied by the user.
    /// </summary>
    Denied = 3,

    /// <summary>
    /// Approval request has expired without action.
    /// </summary>
    Expired = 4
}
