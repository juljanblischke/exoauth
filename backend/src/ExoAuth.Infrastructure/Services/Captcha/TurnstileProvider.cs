using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExoAuth.Infrastructure.Services.Captcha;

/// <summary>
/// Cloudflare Turnstile CAPTCHA provider.
/// </summary>
public sealed class TurnstileProvider : ICaptchaProvider
{
    private readonly HttpClient _httpClient;
    private readonly CaptchaSettings _settings;
    private readonly ILogger<TurnstileProvider> _logger;
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public string ProviderName => "turnstile";

    public TurnstileProvider(
        HttpClient httpClient,
        IOptions<CaptchaSettings> settings,
        ILogger<TurnstileProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<CaptchaResult> ValidateAsync(
        string token,
        string? remoteIp = null,
        string? expectedAction = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return CaptchaResult.Failed("missing-input-response");
        }

        try
        {
            var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = _settings.Turnstile.SecretKey,
                ["response"] = token,
                ["remoteip"] = remoteIp ?? string.Empty
            });

            var response = await _httpClient.PostAsync(VerifyUrl, requestContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>(cancellationToken);

            if (result is null)
            {
                _logger.LogWarning("Turnstile API returned null response");
                return CaptchaResult.Failed("invalid-response");
            }

            if (result.Success)
            {
                _logger.LogDebug("Turnstile validation successful for action {Action}", result.Action);
                return CaptchaResult.Successful(action: result.Action);
            }

            var errorCode = result.ErrorCodes?.FirstOrDefault() ?? "unknown-error";
            _logger.LogWarning("Turnstile validation failed: {ErrorCode}", errorCode);

            return CaptchaResult.Failed(MapErrorCode(errorCode));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Turnstile token");
            return CaptchaResult.Failed("validation-error");
        }
    }

    private static string MapErrorCode(string turnstileCode)
    {
        return turnstileCode switch
        {
            "missing-input-secret" => "configuration-error",
            "invalid-input-secret" => "configuration-error",
            "missing-input-response" => "missing-token",
            "invalid-input-response" => "invalid-token",
            "bad-request" => "invalid-request",
            "timeout-or-duplicate" => "token-expired",
            "internal-error" => "provider-error",
            _ => turnstileCode
        };
    }

    private sealed class TurnstileResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTimestamp { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("cdata")]
        public string? CData { get; set; }
    }
}
