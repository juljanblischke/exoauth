namespace ExoAuth.Domain.Enums;

/// <summary>
/// Represents the status of an email announcement.
/// </summary>
public enum EmailAnnouncementStatus
{
    /// <summary>
    /// Announcement is in draft state and not yet sent.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Announcement is currently being sent to recipients.
    /// </summary>
    Sending = 1,

    /// <summary>
    /// Announcement has been sent to all recipients.
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Announcement was sent but some emails failed.
    /// </summary>
    PartiallyFailed = 3
}
