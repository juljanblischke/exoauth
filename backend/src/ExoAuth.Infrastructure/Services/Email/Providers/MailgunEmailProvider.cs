using System.Net.Http.Headers;
using System.Text;
using ExoAuth.Application.Common.Interfaces;

namespace ExoAuth.Infrastructure.Services.Email.Providers;

public sealed class MailgunEmailProvider : IEmailProviderImplementation
{
    private readonly MailgunConfig _config;
    private readonly HttpClient _httpClient;

    public MailgunEmailProvider(MailgunConfig config, HttpClient httpClient)
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
        var baseUrl = _config.Region.ToUpper() == "EU"
            ? "https://api.eu.mailgun.net"
            : "https://api.mailgun.net";

        var url = $"{baseUrl}/v3/{_config.Domain}/messages";

        var content = new MultipartFormDataContent
        {
            { new StringContent($"{_config.FromName} <{_config.FromEmail}>"), "from" },
            { new StringContent(to), "to" },
            { new StringContent(subject), "subject" },
            { new StringContent(htmlBody), "html" }
        };

        if (!string.IsNullOrWhiteSpace(plainTextBody))
        {
            content.Add(new StringContent(plainTextBody), "text");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        var authBytes = Encoding.ASCII.GetBytes($"api:{_config.ApiKey}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        request.Content = content;

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Mailgun API error: {response.StatusCode} - {error}");
        }
    }
}
