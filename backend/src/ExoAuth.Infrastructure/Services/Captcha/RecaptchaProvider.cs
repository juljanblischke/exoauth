using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExoAuth.Infrastructure.Services.Captcha;

/// <summary>
/// Google reCAPTCHA v3 provider.
/// </summary>
public sealed class RecaptchaProvider : ICaptchaProvider
{
    private readonly HttpClient _httpClient;
    private readonly CaptchaSettings _settings;
    private readonly ILogger<RecaptchaProvider> _logger;
    private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

    public string ProviderName => "recaptcha";

    public RecaptchaProvider(
        HttpClient httpClient,
        IOptions<CaptchaSettings> settings,
        ILogger<RecaptchaProvider> logger)
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
                ["secret"] = _settings.Recaptcha.SecretKey,
                ["response"] = token,
                ["remoteip"] = remoteIp ?? string.Empty
            });

            var response = await _httpClient.PostAsync(VerifyUrl, requestContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<RecaptchaResponse>(cancellationToken);

            if (result is null)
            {
                _logger.LogWarning("reCAPTCHA API returned null response");
                return CaptchaResult.Failed("invalid-response");
            }

            if (!result.Success)
            {
                var errorCode = result.ErrorCodes?.FirstOrDefault() ?? "unknown-error";
                _logger.LogWarning("reCAPTCHA validation failed: {ErrorCode}", errorCode);
                return CaptchaResult.Failed(MapErrorCode(errorCode));
            }

            // Check score threshold for reCAPTCHA v3
            if (result.Score < _settings.Recaptcha.MinScore)
            {
                _logger.LogWarning(
                    "reCAPTCHA score {Score} below threshold {MinScore}",
                    result.Score,
                    _settings.Recaptcha.MinScore);
                return CaptchaResult.Failed("score-below-threshold");
            }

            // Optionally verify action matches
            if (!string.IsNullOrEmpty(expectedAction) && result.Action != expectedAction)
            {
                _logger.LogWarning(
                    "reCAPTCHA action mismatch: expected {Expected}, got {Actual}",
                    expectedAction,
                    result.Action);
                return CaptchaResult.Failed("action-mismatch");
            }

            _logger.LogDebug(
                "reCAPTCHA validation successful: score={Score}, action={Action}",
                result.Score,
                result.Action);

            return CaptchaResult.Successful(result.Score, result.Action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reCAPTCHA token");
            return CaptchaResult.Failed("validation-error");
        }
    }

    private static string MapErrorCode(string recaptchaCode)
    {
        return recaptchaCode switch
        {
            "missing-input-secret" => "configuration-error",
            "invalid-input-secret" => "configuration-error",
            "missing-input-response" => "missing-token",
            "invalid-input-response" => "invalid-token",
            "bad-request" => "invalid-request",
            "timeout-or-duplicate" => "token-expired",
            _ => recaptchaCode
        };
    }

    private sealed class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public float Score { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTimestamp { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
