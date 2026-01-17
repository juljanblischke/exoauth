namespace ExoAuth.Domain.Enums;

/// <summary>
/// Represents the type of email provider.
/// </summary>
public enum EmailProviderType
{
    /// <summary>
    /// Standard SMTP server.
    /// </summary>
    Smtp = 0,

    /// <summary>
    /// SendGrid email service.
    /// </summary>
    SendGrid = 1,

    /// <summary>
    /// Mailgun email service.
    /// </summary>
    Mailgun = 2,

    /// <summary>
    /// Amazon Simple Email Service (SES).
    /// </summary>
    AmazonSes = 3,

    /// <summary>
    /// Resend email service.
    /// </summary>
    Resend = 4,

    /// <summary>
    /// Postmark email service.
    /// </summary>
    Postmark = 5
}
