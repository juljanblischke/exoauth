using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<EmailService> _logger;
    private readonly string _baseUrl;
    private readonly int _inviteExpirationHours;
    private readonly int _passwordResetExpiryMinutes;

    public EmailService(
        IMessageBus messageBus,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _messageBus = messageBus;
        _logger = logger;

        var inviteSection = configuration.GetSection("SystemInvite");
        _baseUrl = inviteSection["BaseUrl"] ?? "https://localhost";
        _inviteExpirationHours = inviteSection.GetValue<int>("ExpirationHours", 24);

        _passwordResetExpiryMinutes = configuration.GetValue("Auth:PasswordResetExpiryMinutes", 15);
    }

    public async Task SendAsync(
        string to,
        string subject,
        string templateName,
        Dictionary<string, string> variables,
        string language = "en-US",
        CancellationToken cancellationToken = default)
    {
        var message = new SendEmailMessage(
            To: to,
            Subject: subject,
            TemplateName: templateName,
            Language: language,
            Variables: variables
        );

        await _messageBus.PublishAsync(message, "email.send", cancellationToken);

        _logger.LogInformation("Queued email to {To} with template {Template}", to, templateName);
    }

    public async Task SendSystemInviteAsync(
        string email,
        string firstName,
        string inviterName,
        string inviteToken,
        string language = "en-US",
        CancellationToken cancellationToken = default)
    {
        var inviteLink = $"{_baseUrl}/invite?token={inviteToken}";

        var variables = new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["inviterName"] = inviterName,
            ["inviteLink"] = inviteLink,
            ["expirationHours"] = _inviteExpirationHours.ToString(),
            ["year"] = DateTime.UtcNow.Year.ToString()
        };

        var subject = language.StartsWith("de")
            ? "Einladung zu ExoAuth"
            : "You're invited to ExoAuth";

        await SendAsync(
            to: email,
            subject: subject,
            templateName: "system-invite",
            variables: variables,
            language: language,
            cancellationToken: cancellationToken
        );
    }

    public async Task SendPasswordResetAsync(
        string email,
        string firstName,
        string resetToken,
        string resetCode,
        string language = "en-US",
        CancellationToken cancellationToken = default)
    {
        var resetUrl = $"{_baseUrl}/auth/reset-password?token={resetToken}";

        var variables = new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["resetUrl"] = resetUrl,
            ["resetCode"] = resetCode,
            ["expiryMinutes"] = _passwordResetExpiryMinutes.ToString(),
            ["year"] = DateTime.UtcNow.Year.ToString()
        };

        var subject = language.StartsWith("de")
            ? "Passwort zurücksetzen"
            : "Reset Your Password";

        await SendAsync(
            to: email,
            subject: subject,
            templateName: "password-reset",
            variables: variables,
            language: language,
            cancellationToken: cancellationToken
        );
    }

    public async Task SendPasswordChangedAsync(
        string email,
        string firstName,
        string language = "en-US",
        CancellationToken cancellationToken = default)
    {
        var variables = new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["year"] = DateTime.UtcNow.Year.ToString()
        };

        var subject = language.StartsWith("de")
            ? "Dein Passwort wurde geändert"
            : "Your Password Has Been Changed";

        await SendAsync(
            to: email,
            subject: subject,
            templateName: "password-changed",
            variables: variables,
            language: language,
            cancellationToken: cancellationToken
        );
    }
}
