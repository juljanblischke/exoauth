using System.Text.Json.Serialization;

namespace ExoAuth.EmailWorker.Models;

/// <summary>
/// Message for sending an email via the message queue.
/// </summary>
public sealed record SendEmailMessage(
    string To,
    string Subject,
    string TemplateName,
    string Language,
    Dictionary<string, string> Variables,
    [property: JsonPropertyName("recipientUserId")] Guid? RecipientUserId = null,
    [property: JsonPropertyName("announcementId")] Guid? AnnouncementId = null,
    [property: JsonPropertyName("htmlBody")] string? HtmlBody = null,
    [property: JsonPropertyName("plainTextBody")] string? PlainTextBody = null,
    [property: JsonPropertyName("existingEmailLogId")] Guid? ExistingEmailLogId = null
);
