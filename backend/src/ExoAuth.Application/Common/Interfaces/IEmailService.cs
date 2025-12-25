namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for sending emails via message queue.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Queues an email to be sent.
    /// </summary>
    /// <param name="to">The recipient email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="templateName">The name of the email template.</param>
    /// <param name="variables">Variables to replace in the template.</param>
    /// <param name="language">The language for the template (default: "en").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(
        string to,
        string subject,
        string templateName,
        Dictionary<string, string> variables,
        string language = "en",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a system user invitation email.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="firstName">The recipient's first name.</param>
    /// <param name="inviterName">The name of the person sending the invite.</param>
    /// <param name="inviteToken">The invitation token.</param>
    /// <param name="language">The language for the template (default: "en").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendSystemInviteAsync(
        string email,
        string firstName,
        string inviterName,
        string inviteToken,
        string language = "en",
        CancellationToken cancellationToken = default);
}
