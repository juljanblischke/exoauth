using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.Infrastructure.Services.Email.Providers;

namespace ExoAuth.Infrastructure.Services.Email;

public sealed class EmailProviderFactory : IEmailProviderFactory
{
    private readonly IEncryptionService _encryptionService;
    private readonly IHttpClientFactory _httpClientFactory;

    public EmailProviderFactory(
        IEncryptionService encryptionService,
        IHttpClientFactory httpClientFactory)
    {
        _encryptionService = encryptionService;
        _httpClientFactory = httpClientFactory;
    }

    public IEmailProviderImplementation CreateProvider(EmailProvider provider)
    {
        var configJson = _encryptionService.Decrypt(provider.ConfigurationEncrypted);

        return provider.Type switch
        {
            EmailProviderType.Smtp => CreateSmtpProvider(configJson),
            EmailProviderType.SendGrid => CreateSendGridProvider(configJson),
            EmailProviderType.Mailgun => CreateMailgunProvider(configJson),
            EmailProviderType.AmazonSes => CreateAmazonSesProvider(configJson),
            EmailProviderType.Resend => CreateResendProvider(configJson),
            EmailProviderType.Postmark => CreatePostmarkProvider(configJson),
            _ => throw new ArgumentOutOfRangeException(nameof(provider.Type), $"Unsupported provider type: {provider.Type}")
        };
    }

    private SmtpEmailProvider CreateSmtpProvider(string configJson)
    {
        var config = JsonSerializer.Deserialize<SmtpConfig>(configJson)
            ?? throw new InvalidOperationException("Invalid SMTP configuration");
        return new SmtpEmailProvider(config);
    }

    private SendGridEmailProvider CreateSendGridProvider(string configJson)
    {
        var config = JsonSerializer.Deserialize<SendGridConfig>(configJson)
            ?? throw new InvalidOperationException("Invalid SendGrid configuration");
        var httpClient = _httpClientFactory.CreateClient("SendGrid");
        return new SendGridEmailProvider(config, httpClient);
    }

    private IEmailProviderImplementation CreateMailgunProvider(string configJson)
    {
        var config = JsonSerializer.Deserialize<MailgunConfig>(configJson)
            ?? throw new InvalidOperationException("Invalid Mailgun configuration");
        var httpClient = _httpClientFactory.CreateClient("Mailgun");
        return new MailgunEmailProvider(config, httpClient);
    }

    private IEmailProviderImplementation CreateAmazonSesProvider(string configJson)
    {
        var config = JsonSerializer.Deserialize<AmazonSesConfig>(configJson)
            ?? throw new InvalidOperationException("Invalid Amazon SES configuration");
        var httpClient = _httpClientFactory.CreateClient("AmazonSES");
        return new AmazonSesEmailProvider(config, httpClient);
    }

    private IEmailProviderImplementation CreateResendProvider(string configJson)
    {
        var config = JsonSerializer.Deserialize<ResendConfig>(configJson)
            ?? throw new InvalidOperationException("Invalid Resend configuration");
        var httpClient = _httpClientFactory.CreateClient("Resend");
        return new ResendEmailProvider(config, httpClient);
    }

    private IEmailProviderImplementation CreatePostmarkProvider(string configJson)
    {
        var config = JsonSerializer.Deserialize<PostmarkConfig>(configJson)
            ?? throw new InvalidOperationException("Invalid Postmark configuration");
        var httpClient = _httpClientFactory.CreateClient("Postmark");
        return new PostmarkEmailProvider(config, httpClient);
    }
}
