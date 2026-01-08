using System.Net;
using System.Net.Mail;
using ExoAuth.Application.Common.Interfaces;

namespace ExoAuth.Infrastructure.Services.Email.Providers;

public sealed class SmtpEmailProvider : IEmailProviderImplementation
{
    private readonly SmtpConfig _config;

    public SmtpEmailProvider(SmtpConfig config)
    {
        _config = config;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        string? plainTextBody,
        CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_config.Host, _config.Port)
        {
            EnableSsl = _config.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrEmpty(_config.Username))
        {
            client.Credentials = new NetworkCredential(_config.Username, _config.Password);
        }

        var fromAddress = new MailAddress(_config.FromEmail, _config.FromName);
        var toAddress = new MailAddress(to);

        using var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        if (!string.IsNullOrWhiteSpace(plainTextBody))
        {
            var plainView = AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain");
            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
            message.AlternateViews.Add(plainView);
            message.AlternateViews.Add(htmlView);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
