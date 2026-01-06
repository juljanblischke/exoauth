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
    /// <param name="language">The language for the template (default: "en-US").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(
        string to,
        string subject,
        string templateName,
        Dictionary<string, string> variables,
        string language = "en-US",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a system user invitation email.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="firstName">The recipient's first name.</param>
    /// <param name="inviterName">The name of the person sending the invite.</param>
    /// <param name="inviteToken">The invitation token.</param>
    /// <param name="language">The language for the template (default: "en-US").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendSystemInviteAsync(
        string email,
        string firstName,
        string inviterName,
        string inviteToken,
        string language = "en-US",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset email with token and code.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="firstName">The recipient's first name.</param>
    /// <param name="resetToken">The URL token for reset link.</param>
    /// <param name="resetCode">The XXXX-XXXX code for manual entry.</param>
    /// <param name="language">The language for the template (default: "en-US").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordResetAsync(
        string email,
        string firstName,
        string resetToken,
        string resetCode,
        string language = "en-US",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password changed confirmation email.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="firstName">The recipient's first name.</param>
    /// <param name="language">The language for the template (default: "en-US").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordChangedAsync(
        string email,
        string firstName,
        string language = "en-US",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a device approval required email with approval link and code.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="firstName">The recipient's first name.</param>
    /// <param name="approvalToken">The URL token for approval link.</param>
    /// <param name="approvalCode">The XXXX-XXXX code for manual entry.</param>
    /// <param name="deviceName">Name of the device requesting approval.</param>
    /// <param name="browser">Browser name.</param>
    /// <param name="operatingSystem">Operating system name.</param>
    /// <param name="location">Location of the login attempt.</param>
    /// <param name="ipAddress">IP address of the login attempt.</param>
    /// <param name="riskScore">The calculated risk score.</param>
    /// <param name="language">The language for the template (default: "en-US").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendDeviceApprovalRequiredAsync(
        string email,
        string firstName,
        string approvalToken,
        string approvalCode,
        string? deviceName,
        string? browser,
        string? operatingSystem,
        string? location,
        string? ipAddress,
        int riskScore,
        string language = "en-US",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a security alert when a device approval is denied.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="firstName">The recipient's first name.</param>
    /// <param name="deviceName">Name of the denied device.</param>
    /// <param name="browser">Browser name.</param>
    /// <param name="operatingSystem">Operating system name.</param>
    /// <param name="location">Location of the denied attempt.</param>
    /// <param name="ipAddress">IP address of the denied attempt.</param>
    /// <param name="language">The language for the template (default: "en-US").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendDeviceDeniedAlertAsync(
        string email,
        string firstName,
        string? deviceName,
        string? browser,
        string? operatingSystem,
        string? location,
        string? ipAddress,
        string language = "en-US",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a confirmation email when a passkey is registered.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="fullName">The recipient's full name.</param>
    /// <param name="passkeyName">The name of the registered passkey.</param>
    /// <param name="language">The language for the template (default: "en-US").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasskeyRegisteredEmailAsync(
        string email,
        string fullName,
        string passkeyName,
        string language = "en-US",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a security alert when a passkey is removed.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="fullName">The recipient's full name.</param>
    /// <param name="passkeyName">The name of the removed passkey.</param>
    /// <param name="language">The language for the template (default: "en-US").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasskeyRemovedEmailAsync(
        string email,
        string fullName,
        string passkeyName,
        string language = "en-US",
        CancellationToken cancellationToken = default);
}
