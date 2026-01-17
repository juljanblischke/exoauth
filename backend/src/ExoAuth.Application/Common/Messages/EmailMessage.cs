using System.Text.Json.Serialization;

namespace ExoAuth.Application.Common.Messages;

/// <summary>
/// Message for sending an email via the message queue.
/// </summary>
public sealed record SendEmailMessage(
    [property: JsonPropertyName("to")] string To,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("templateName")] string TemplateName,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("variables")] Dictionary<string, string> Variables,
    [property: JsonPropertyName("recipientUserId")] Guid? RecipientUserId = null,
    [property: JsonPropertyName("announcementId")] Guid? AnnouncementId = null,
    [property: JsonPropertyName("htmlBody")] string? HtmlBody = null,
    [property: JsonPropertyName("plainTextBody")] string? PlainTextBody = null,
    [property: JsonPropertyName("existingEmailLogId")] Guid? ExistingEmailLogId = null
);
