namespace ExoAuth.Application.Common.Interfaces;

public interface IEmailProviderImplementation
{
    Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        string? plainTextBody,
        CancellationToken cancellationToken = default);
}

public record EmailProviderConfig
{
    public string FromEmail { get; init; } = null!;
    public string FromName { get; init; } = null!;
}

public record SmtpConfig : EmailProviderConfig
{
    public string Host { get; init; } = null!;
    public int Port { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool UseSsl { get; init; }
}

public record SendGridConfig : EmailProviderConfig
{
    public string ApiKey { get; init; } = null!;
}

public record MailgunConfig : EmailProviderConfig
{
    public string ApiKey { get; init; } = null!;
    public string Domain { get; init; } = null!;
    public string Region { get; init; } = "US"; // US or EU
}

public record AmazonSesConfig : EmailProviderConfig
{
    public string AccessKey { get; init; } = null!;
    public string SecretKey { get; init; } = null!;
    public string Region { get; init; } = null!;
}

public record ResendConfig : EmailProviderConfig
{
    public string ApiKey { get; init; } = null!;
}

public record PostmarkConfig : EmailProviderConfig
{
    public string ServerToken { get; init; } = null!;
}
