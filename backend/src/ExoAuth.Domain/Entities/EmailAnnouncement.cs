using ExoAuth.Domain.Enums;

namespace ExoAuth.Domain.Entities;

/// <summary>
/// Represents an email announcement that can be sent to multiple users.
/// </summary>
public sealed class EmailAnnouncement : BaseEntity
{
    /// <summary>
    /// The subject line of the announcement email.
    /// </summary>
    public string Subject { get; private set; } = null!;

    /// <summary>
    /// The HTML body of the announcement email.
    /// </summary>
    public string HtmlBody { get; private set; } = null!;

    /// <summary>
    /// The plain text body of the announcement email (optional fallback).
    /// </summary>
    public string? PlainTextBody { get; private set; }

    // Targeting
    /// <summary>
    /// The targeting method for recipients.
    /// </summary>
    public EmailAnnouncementTarget TargetType { get; private set; }

    /// <summary>
    /// The permission code to filter recipients (when TargetType is ByPermission).
    /// </summary>
    public string? TargetPermission { get; private set; }

    /// <summary>
    /// JSON array of user IDs (when TargetType is SelectedUsers).
    /// </summary>
    public string? TargetUserIds { get; private set; }

    // Statistics
    /// <summary>
    /// Total number of recipients for this announcement.
    /// </summary>
    public int TotalRecipients { get; private set; }

    /// <summary>
    /// Number of emails successfully sent.
    /// </summary>
    public int SentCount { get; private set; }

    /// <summary>
    /// Number of emails that failed to send.
    /// </summary>
    public int FailedCount { get; private set; }

    // Metadata
    /// <summary>
    /// The user ID of the admin who created this announcement.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// When the announcement was sent (or started sending).
    /// </summary>
    public DateTime? SentAt { get; private set; }

    /// <summary>
    /// Current status of the announcement.
    /// </summary>
    public EmailAnnouncementStatus Status { get; private set; }

    // Navigation Properties
    public SystemUser? CreatedByUser { get; set; }
    public ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();

    private EmailAnnouncement() { } // EF Core

    /// <summary>
    /// Creates a new announcement targeting all users.
    /// </summary>
    public static EmailAnnouncement CreateForAllUsers(
        string subject,
        string htmlBody,
        Guid createdByUserId,
        string? plainTextBody = null)
    {
        return new EmailAnnouncement
        {
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody,
            TargetType = EmailAnnouncementTarget.AllUsers,
            CreatedByUserId = createdByUserId,
            Status = EmailAnnouncementStatus.Draft,
            TotalRecipients = 0,
            SentCount = 0,
            FailedCount = 0
        };
    }

    /// <summary>
    /// Creates a new announcement targeting users with a specific permission.
    /// </summary>
    public static EmailAnnouncement CreateForPermission(
        string subject,
        string htmlBody,
        string targetPermission,
        Guid createdByUserId,
        string? plainTextBody = null)
    {
        return new EmailAnnouncement
        {
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody,
            TargetType = EmailAnnouncementTarget.ByPermission,
            TargetPermission = targetPermission,
            CreatedByUserId = createdByUserId,
            Status = EmailAnnouncementStatus.Draft,
            TotalRecipients = 0,
            SentCount = 0,
            FailedCount = 0
        };
    }

    /// <summary>
    /// Creates a new announcement targeting specific users.
    /// </summary>
    public static EmailAnnouncement CreateForSelectedUsers(
        string subject,
        string htmlBody,
        string targetUserIds,
        Guid createdByUserId,
        string? plainTextBody = null)
    {
        return new EmailAnnouncement
        {
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody,
            TargetType = EmailAnnouncementTarget.SelectedUsers,
            TargetUserIds = targetUserIds,
            CreatedByUserId = createdByUserId,
            Status = EmailAnnouncementStatus.Draft,
            TotalRecipients = 0,
            SentCount = 0,
            FailedCount = 0
        };
    }

    /// <summary>
    /// Updates the announcement content (only allowed when in Draft status).
    /// </summary>
    public void Update(
        string subject,
        string htmlBody,
        string? plainTextBody,
        EmailAnnouncementTarget targetType,
        string? targetPermission,
        string? targetUserIds)
    {
        if (Status != EmailAnnouncementStatus.Draft)
            throw new InvalidOperationException("Cannot modify an announcement that has already been sent.");

        Subject = subject;
        HtmlBody = htmlBody;
        PlainTextBody = plainTextBody;
        TargetType = targetType;
        TargetPermission = targetPermission;
        TargetUserIds = targetUserIds;
        SetUpdated();
    }

    /// <summary>
    /// Starts sending the announcement.
    /// </summary>
    public void StartSending(int totalRecipients)
    {
        if (Status != EmailAnnouncementStatus.Draft)
            throw new InvalidOperationException("Cannot send an announcement that has already been sent.");

        Status = EmailAnnouncementStatus.Sending;
        TotalRecipients = totalRecipients;
        SentAt = DateTime.UtcNow;
        SetUpdated();
    }

    /// <summary>
    /// Marks the announcement as fully sent.
    /// </summary>
    public void MarkSent()
    {
        Status = FailedCount > 0
            ? EmailAnnouncementStatus.PartiallyFailed
            : EmailAnnouncementStatus.Sent;
        SetUpdated();
    }

    /// <summary>
    /// Increments the sent count.
    /// </summary>
    public void IncrementSentCount()
    {
        SentCount++;
        UpdateStatusIfComplete();
        SetUpdated();
    }

    /// <summary>
    /// Increments the failed count.
    /// </summary>
    public void IncrementFailedCount()
    {
        FailedCount++;
        UpdateStatusIfComplete();
        SetUpdated();
    }

    /// <summary>
    /// Updates the status if all emails have been processed.
    /// </summary>
    private void UpdateStatusIfComplete()
    {
        if (SentCount + FailedCount >= TotalRecipients)
        {
            Status = FailedCount > 0
                ? EmailAnnouncementStatus.PartiallyFailed
                : EmailAnnouncementStatus.Sent;
        }
    }

    /// <summary>
    /// Updates the statistics.
    /// </summary>
    public void UpdateStats(int sentCount, int failedCount)
    {
        SentCount = sentCount;
        FailedCount = failedCount;

        // Determine final status
        if (SentCount + FailedCount >= TotalRecipients)
        {
            Status = FailedCount > 0
                ? EmailAnnouncementStatus.PartiallyFailed
                : EmailAnnouncementStatus.Sent;
        }

        SetUpdated();
    }

    /// <summary>
    /// Checks if the announcement is a draft.
    /// </summary>
    public bool IsDraft => Status == EmailAnnouncementStatus.Draft;

    /// <summary>
    /// Checks if the announcement can be modified.
    /// </summary>
    public bool CanBeModified => Status == EmailAnnouncementStatus.Draft;

    /// <summary>
    /// Checks if the announcement can be deleted.
    /// </summary>
    public bool CanBeDeleted => Status == EmailAnnouncementStatus.Draft;

    /// <summary>
    /// Gets the progress percentage.
    /// </summary>
    public double Progress => TotalRecipients == 0 ? 0 : (double)(SentCount + FailedCount) / TotalRecipients * 100;
}
