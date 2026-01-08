namespace ExoAuth.Domain.Enums;

/// <summary>
/// Represents the status of an email in the system.
/// </summary>
public enum EmailStatus
{
    /// <summary>
    /// Email is queued for sending.
    /// </summary>
    Queued = 0,

    /// <summary>
    /// Email is currently being sent.
    /// </summary>
    Sending = 1,

    /// <summary>
    /// Email was successfully sent.
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Email failed to send after all retries.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Email is in the dead letter queue after all providers failed.
    /// </summary>
    InDlq = 4,

    /// <summary>
    /// Email was retried from the dead letter queue.
    /// </summary>
    RetriedFromDlq = 5
}
