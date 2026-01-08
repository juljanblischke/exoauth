using System.Net.Http.Json;
using ExoAuth.Application.Common.Interfaces;

namespace ExoAuth.Infrastructure.Services.Email.Providers;

public sealed class PostmarkEmailProvider : IEmailProviderImplementation
{
    private readonly PostmarkConfig _config;
    private readonly HttpClient _httpClient;

    public PostmarkEmailProvider(PostmarkConfig config, HttpClient httpClient)
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
        var payload = new
        {
            From = $"{_config.FromName} <{_config.FromEmail}>",
            To = to,
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = plainTextBody,
            MessageStream = "outbound"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.postmarkapp.com/email");
        request.Headers.Add("X-Postmark-Server-Token", _config.ServerToken);
        request.Headers.Add("Accept", "application/json");
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Postmark API error: {response.StatusCode} - {error}");
        }
    }
}
