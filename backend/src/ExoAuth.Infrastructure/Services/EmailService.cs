using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly IMessageBus _messageBus;
    private readonly IEmailTemplateService _templateService;
    private readonly ILogger<EmailService> _logger;
    private readonly string _baseUrl;
    private readonly int _inviteExpirationHours;
    private readonly int _passwordResetExpiryMinutes;
    private readonly int _deviceApprovalExpiryMinutes;
    private readonly int _magicLinkExpiryMinutes;

    public EmailService(
        IMessageBus messageBus,
        IEmailTemplateService templateService,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _messageBus = messageBus;
        _templateService = templateService;
        _logger = logger;

        var inviteSection = configuration.GetSection("SystemInvite");
        _baseUrl = inviteSection["BaseUrl"] ?? "https://localhost";
        _inviteExpirationHours = inviteSection.GetValue<int>("ExpirationHours", 24);

        _passwordResetExpiryMinutes = configuration.GetValue("Auth:PasswordResetExpiryMinutes", 15);
        _deviceApprovalExpiryMinutes = configuration.GetValue("DeviceTrust:ApprovalExpiryMinutes", 30);
        _magicLinkExpiryMinutes = configuration.GetValue("Auth:MagicLinkExpiryMinutes", 15);
    }

    public async Task SendAsync(
        string to,
        string subject,
        string templateName,
        Dictionary<string, string> variables,
        string language = "en-US",
        Guid? recipientUserId = null,
        CancellationToken cancellationToken = default)
    {
        var message = new SendEmailMessage(
            To: to,
            Subject: subject,
            TemplateName: templateName,
            Language: language,
            Variables: variables,
            RecipientUserId: recipientUserId
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

        await SendAsync(
            to: email,
            subject: _templateService.GetSubject("system-invite", language),
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
        Guid userId,
        string language = "en-US",
        CancellationToken cancellationToken = default)
    {
        var resetUrl = $"{_baseUrl}/reset-password?token={resetToken}";

        var variables = new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["resetUrl"] = resetUrl,
            ["resetCode"] = resetCode,
            ["expiryMinutes"] = _passwordResetExpiryMinutes.ToString(),
            ["year"] = DateTime.UtcNow.Year.ToString()
        };

        await SendAsync(
            to: email,
            subject: _templateService.GetSubject("password-reset", language),
            templateName: "password-reset",
            variables: variables,
            language: language,
            recipientUserId: userId,
            cancellationToken: cancellationToken
        );
    }

    public async Task SendMagicLinkAsync(
        string email,
        string firstName,
        string magicLinkToken,
        Guid userId,
        string language = "en-US",
        CancellationToken cancellationToken = default)
    {
        var magicLinkUrl = $"{_baseUrl}/magic-link-login?token={magicLinkToken}";

        var variables = new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["magicLinkUrl"] = magicLinkUrl,
            ["expirationMinutes"] = _magicLinkExpiryMinutes.ToString(),
            ["year"] = DateTime.UtcNow.Year.ToString()
        };

        await SendAsync(
            to: email,
            subject: _templateService.GetSubject("magic-link", language),
            templateName: "magic-link",
            variables: variables,
            language: language,
            recipientUserId: userId,
            cancellationToken: cancellationToken
        );
    }

    public async Task SendPasswordChangedAsync(
        string email,
        string firstName,
        Guid userId,
        string language = "en-US",
        CancellationToken cancellationToken = default)
    {
        var variables = new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["year"] = DateTime.UtcNow.Year.ToString()
        };

        await SendAsync(
            to: email,
            subject: _templateService.GetSubject("password-changed", language),
            templateName: "password-changed",
            variables: variables,
            language: language,
            recipientUserId: userId,
            cancellationToken: cancellationToken
        );
    }

    public async Task SendDeviceApprovalRequiredAsync(
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
        Guid userId,
        string language = "en-US",
        CancellationToken cancellationToken = default)
    {
        var approvalUrl = $"{_baseUrl}/approve-device/{approvalToken}";
        var denyUrl = $"{_baseUrl}/deny-device/{approvalToken}";

        var variables = new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["approvalUrl"] = approvalUrl,
            ["denyUrl"] = denyUrl,
            ["approvalCode"] = approvalCode,
            ["deviceName"] = deviceName ?? "Unknown Device",
            ["browser"] = browser ?? "Unknown Browser",
            ["operatingSystem"] = operatingSystem ?? "Unknown OS",
            ["location"] = location ?? "Unknown Location",
            ["ipAddress"] = ipAddress ?? "Unknown",
            ["riskScore"] = riskScore.ToString(),
            ["expiryMinutes"] = _deviceApprovalExpiryMinutes.ToString(),
            ["year"] = DateTime.UtcNow.Year.ToString()
        };

        await SendAsync(
            to: email,
            subject: _templateService.GetSubject("device-approval-required", language),
            templateName: "device-approval-required",
            variables: variables,
            language: language,
            recipientUserId: userId,
            cancellationToken: cancellationToken
        );
    }

    public async Task SendDeviceDeniedAlertAsync(
        string email,
        string firstName,
        string? deviceName,
        string? browser,
        string? operatingSystem,
        string? location,
        string? ipAddress,
        Guid userId,
        string language = "en-US",
        CancellationToken cancellationToken = default)
    {
        var variables = new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["deviceName"] = deviceName ?? "Unknown Device",
            ["browser"] = browser ?? "Unknown Browser",
            ["operatingSystem"] = operatingSystem ?? "Unknown OS",
            ["location"] = location ?? "Unknown Location",
            ["ipAddress"] = ipAddress ?? "Unknown",
            ["deniedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            ["year"] = DateTime.UtcNow.Year.ToString()
        };

        await SendAsync(
            to: email,
            subject: _templateService.GetSubject("device-denied-alert", language),
            templateName: "device-denied-alert",
            variables: variables,
            language: language,
            recipientUserId: userId,
            cancellationToken: cancellationToken
        );
    }

    public async Task SendPasskeyRegisteredEmailAsync(
        string email,
        string fullName,
        string passkeyName,
        Guid userId,
        string language = "en-US",
        CancellationToken cancellationToken = default)
    {
        var variables = new Dictionary<string, string>
        {
            ["fullName"] = fullName,
            ["passkeyName"] = passkeyName,
            ["registeredAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            ["year"] = DateTime.UtcNow.Year.ToString()
        };

        await SendAsync(
            to: email,
            subject: _templateService.GetSubject("passkey-registered", language),
            templateName: "passkey-registered",
            variables: variables,
            language: language,
            recipientUserId: userId,
            cancellationToken: cancellationToken
        );
    }

    public async Task SendPasskeyRemovedEmailAsync(
        string email,
        string fullName,
        string passkeyName,
        Guid userId,
        string language = "en-US",
        CancellationToken cancellationToken = default)
    {
        var variables = new Dictionary<string, string>
        {
            ["fullName"] = fullName,
            ["passkeyName"] = passkeyName,
            ["removedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            ["year"] = DateTime.UtcNow.Year.ToString()
        };

        await SendAsync(
            to: email,
            subject: _templateService.GetSubject("passkey-removed", language),
            templateName: "passkey-removed",
            variables: variables,
            language: language,
            recipientUserId: userId,
            cancellationToken: cancellationToken
        );
    }
}
