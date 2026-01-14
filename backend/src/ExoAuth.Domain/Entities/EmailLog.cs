using ExoAuth.Domain.Enums;

namespace ExoAuth.Domain.Entities;

/// <summary>
/// Represents a log entry for an email that was sent or attempted to be sent.
/// </summary>
public sealed class EmailLog : BaseEntity
{
    /// <summary>
    /// The user ID of the recipient (null for external recipients).
    /// </summary>
    public Guid? RecipientUserId { get; private set; }

    /// <summary>
    /// The email address of the recipient.
    /// </summary>
    public string RecipientEmail { get; private set; } = null!;

    /// <summary>
    /// The subject line of the email.
    /// </summary>
    public string Subject { get; private set; } = null!;

    /// <summary>
    /// The name of the template used (e.g., "password-reset", "system-invite").
    /// </summary>
    public string TemplateName { get; private set; } = null!;

    /// <summary>
    /// JSON representation of template variables (for debugging purposes).
    /// </summary>
    public string? TemplateVariables { get; private set; }

    /// <summary>
    /// The language/locale of the email (e.g., "en-US", "de-DE").
    /// </summary>
    public string Language { get; private set; } = null!;

    // Status Tracking
    /// <summary>
    /// Current status of the email.
    /// </summary>
    public EmailStatus Status { get; private set; }

    /// <summary>
    /// Number of retry attempts made.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// The last error message if the email failed.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// The ID of the provider that successfully sent the email.
    /// </summary>
    public Guid? SentViaProviderId { get; private set; }

    // Timestamps
    /// <summary>
    /// When the email was queued for sending.
    /// </summary>
    public DateTime QueuedAt { get; private set; }

    /// <summary>
    /// When the email was successfully sent.
    /// </summary>
    public DateTime? SentAt { get; private set; }

    /// <summary>
    /// When the email failed (all retries exhausted).
    /// </summary>
    public DateTime? FailedAt { get; private set; }

    /// <summary>
    /// When the email was moved to the DLQ.
    /// </summary>
    public DateTime? MovedToDlqAt { get; private set; }

    /// <summary>
    /// Link to the announcement if this email was part of one.
    /// </summary>
    public Guid? AnnouncementId { get; private set; }

    // Navigation Properties
    public SystemUser? RecipientUser { get; set; }
    public EmailProvider? SentViaProvider { get; set; }
    public EmailAnnouncement? Announcement { get; set; }

    private EmailLog() { } // EF Core

    /// <summary>
    /// Creates a new email log entry in queued status.
    /// </summary>
    public static EmailLog Create(
        string recipientEmail,
        string subject,
        string templateName,
        string language,
        Guid? recipientUserId = null,
        string? templateVariables = null,
        Guid? announcementId = null)
    {
        var now = DateTime.UtcNow;
        return new EmailLog
        {
            RecipientEmail = recipientEmail,
            Subject = subject,
            TemplateName = templateName,
            Language = language,
            RecipientUserId = recipientUserId,
            TemplateVariables = templateVariables,
            AnnouncementId = announcementId,
            Status = EmailStatus.Queued,
            RetryCount = 0,
            QueuedAt = now
        };
    }

    /// <summary>
    /// Marks the email as being sent.
    /// </summary>
    public void MarkSending()
    {
        Status = EmailStatus.Sending;
        SetUpdated();
    }

    /// <summary>
    /// Marks the email as successfully sent.
    /// </summary>
    public void MarkSent(Guid providerId)
    {
        Status = EmailStatus.Sent;
        SentViaProviderId = providerId;
        SentAt = DateTime.UtcNow;
        LastError = null;
        SetUpdated();
    }

    /// <summary>
    /// Marks the email as failed.
    /// </summary>
    public void MarkFailed(string error)
    {
        Status = EmailStatus.Failed;
        FailedAt = DateTime.UtcNow;
        LastError = error;
        SetUpdated();
    }

    /// <summary>
    /// Moves the email to the dead letter queue.
    /// </summary>
    public void MoveToDlq(string error)
    {
        Status = EmailStatus.InDlq;
        MovedToDlqAt = DateTime.UtcNow;
        LastError = error;
        SetUpdated();
    }

    /// <summary>
    /// Requeues the email for sending (used when retrying from DLQ).
    /// </summary>
    public void Requeue()
    {
        Status = EmailStatus.Queued;
        RetryCount = 0;
        LastError = null;
        SentViaProviderId = null;
        SentAt = null;
        FailedAt = null;
        MovedToDlqAt = null;
        QueuedAt = DateTime.UtcNow;
        SetUpdated();
    }

    /// <summary>
    /// Increments the retry count.
    /// </summary>
    public void IncrementRetryCount()
    {
        RetryCount++;
        SetUpdated();
    }

    /// <summary>
    /// Records a retry attempt with error.
    /// </summary>
    public void RecordRetryAttempt(string error)
    {
        RetryCount++;
        LastError = error;
        SetUpdated();
    }

    /// <summary>
    /// Checks if the email is in the DLQ.
    /// </summary>

    /// <summary>
    /// Anonymizes personal data in the email log (GDPR compliance).
    /// </summary>
    public void Anonymize()
    {
        // Keep RecipientUserId so queries can join with SystemUser and show
        // anonymized user info (consistent with audit log behavior).
        // Use same email format as SystemUser.Anonymize() for consistency.
        if (RecipientUserId.HasValue)
        {
            RecipientEmail = $"anonymized_{RecipientUserId.Value:N}@deleted.local";
        }
        else
        {
            RecipientEmail = "anonymized@deleted.local";
        }
        TemplateVariables = null;
        SetUpdated();
    }

    public bool IsInDlq => Status == EmailStatus.InDlq;

    /// <summary>
    /// Checks if the email was successfully sent.
    /// </summary>
    public bool WasSent => Status == EmailStatus.Sent;

    /// <summary>
    /// Checks if the email can be retried.
    /// </summary>
    public bool CanRetry => Status == EmailStatus.InDlq || Status == EmailStatus.Failed;
}
