namespace ExoAuth.Domain.Enums;

/// <summary>
/// Represents the target audience for an email announcement.
/// </summary>
public enum EmailAnnouncementTarget
{
    /// <summary>
    /// Send to all active users.
    /// </summary>
    AllUsers = 0,

    /// <summary>
    /// Send to users with a specific permission.
    /// </summary>
    ByPermission = 1,

    /// <summary>
    /// Send to specifically selected users.
    /// </summary>
    SelectedUsers = 2
}
