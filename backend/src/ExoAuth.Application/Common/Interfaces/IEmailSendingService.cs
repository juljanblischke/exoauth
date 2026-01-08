namespace ExoAuth.Application.Common.Interfaces;

public interface IEmailSendingService
{
    Task<EmailSendResult> SendWithFailoverAsync(
        string recipientEmail,
        Guid? recipientUserId,
        string subject,
        string htmlBody,
        string? plainTextBody,
        string templateName,
        string? templateVariables,
        string language,
        Guid? announcementId = null,
        CancellationToken cancellationToken = default);
}

public record EmailSendResult(
    bool Success,
    Guid EmailLogId,
    Guid? SentViaProviderId,
    string? Error);
