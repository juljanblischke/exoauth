using System.Net.Http.Json;
using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;

namespace ExoAuth.Infrastructure.Services.Email.Providers;

public sealed class SendGridEmailProvider : IEmailProviderImplementation
{
    private readonly SendGridConfig _config;
    private readonly HttpClient _httpClient;

    public SendGridEmailProvider(SendGridConfig config, HttpClient httpClient)
    {
        _config = config;
        _httpClient = httpClient;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        string? plainTextBody,
        CancellationToken cancellationToken = default)
    {
        var content = new List<object>();

        if (!string.IsNullOrWhiteSpace(plainTextBody))
        {
            content.Add(new { type = "text/plain", value = plainTextBody });
        }

        content.Add(new { type = "text/html", value = htmlBody });

        var payload = new
        {
            personalizations = new[]
            {
                new { to = new[] { new { email = to } } }
            },
            from = new
            {
                email = _config.FromEmail,
                name = _config.FromName
            },
            subject = subject,
            content = content
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
        request.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"SendGrid API error: {response.StatusCode} - {error}");
        }
    }
}
