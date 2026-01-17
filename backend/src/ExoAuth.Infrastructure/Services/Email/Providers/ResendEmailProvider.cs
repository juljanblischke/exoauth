using System.Net.Http.Json;
using ExoAuth.Application.Common.Interfaces;

namespace ExoAuth.Infrastructure.Services.Email.Providers;

public sealed class ResendEmailProvider : IEmailProviderImplementation
{
    private readonly ResendConfig _config;
    private readonly HttpClient _httpClient;

    public ResendEmailProvider(ResendConfig config, HttpClient httpClient)
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
            from = $"{_config.FromName} <{_config.FromEmail}>",
            to = new[] { to },
            subject = subject,
            html = htmlBody,
            text = plainTextBody
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Resend API error: {response.StatusCode} - {error}");
        }
    }
}
