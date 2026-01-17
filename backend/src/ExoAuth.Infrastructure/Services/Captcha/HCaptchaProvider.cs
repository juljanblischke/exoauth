using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExoAuth.Infrastructure.Services.Captcha;

/// <summary>
/// hCaptcha provider.
/// </summary>
public sealed class HCaptchaProvider : ICaptchaProvider
{
    private readonly HttpClient _httpClient;
    private readonly CaptchaSettings _settings;
    private readonly ILogger<HCaptchaProvider> _logger;
    private const string VerifyUrl = "https://hcaptcha.com/siteverify";

    public string ProviderName => "hcaptcha";

    public HCaptchaProvider(
        HttpClient httpClient,
        IOptions<CaptchaSettings> settings,
        ILogger<HCaptchaProvider> logger)
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
                ["secret"] = _settings.HCaptcha.SecretKey,
                ["response"] = token,
                ["remoteip"] = remoteIp ?? string.Empty,
                ["sitekey"] = _settings.HCaptcha.SiteKey
            });

            var response = await _httpClient.PostAsync(VerifyUrl, requestContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HCaptchaResponse>(cancellationToken);

            if (result is null)
            {
                _logger.LogWarning("hCaptcha API returned null response");
                return CaptchaResult.Failed("invalid-response");
            }

            if (result.Success)
            {
                _logger.LogDebug("hCaptcha validation successful");
                return CaptchaResult.Successful();
            }

            var errorCode = result.ErrorCodes?.FirstOrDefault() ?? "unknown-error";
            _logger.LogWarning("hCaptcha validation failed: {ErrorCode}", errorCode);

            return CaptchaResult.Failed(MapErrorCode(errorCode));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating hCaptcha token");
            return CaptchaResult.Failed("validation-error");
        }
    }

    private static string MapErrorCode(string hcaptchaCode)
    {
        return hcaptchaCode switch
        {
            "missing-input-secret" => "configuration-error",
            "invalid-input-secret" => "configuration-error",
            "missing-input-response" => "missing-token",
            "invalid-input-response" => "invalid-token",
            "bad-request" => "invalid-request",
            "invalid-or-already-seen-response" => "token-expired",
            "not-using-dummy-passcode" => "test-mode-error",
            "sitekey-secret-mismatch" => "configuration-error",
            _ => hcaptchaCode
        };
    }

    private sealed class HCaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTimestamp { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }

        [JsonPropertyName("credit")]
        public bool? Credit { get; set; }
    }
}
