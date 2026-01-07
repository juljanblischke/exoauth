using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// CAPTCHA service with smart triggering logic.
/// </summary>
public sealed class CaptchaService : ICaptchaService
{
    private readonly ICaptchaProvider _provider;
    private readonly ICacheService _cache;
    private readonly CaptchaSettings _settings;
    private readonly ILogger<CaptchaService> _logger;

    private const string LoginAttemptsKeyPrefix = "login:attempts:";
    private const string DeviceApprovalAttemptsKeyPrefix = "device:approval:attempts:";
    private const string MfaVerifyAttemptsKeyPrefix = "mfa:verify:attempts:";

    public CaptchaService(
        ICaptchaProvider provider,
        ICacheService cache,
        IOptions<CaptchaSettings> settings,
        ILogger<CaptchaService> logger)
    {
        _provider = provider;
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsEnabled => _settings.Enabled;

    public async Task ValidateRequiredAsync(
        string? token,
        string action,
        string? remoteIp = null,
        CancellationToken cancellationToken = default)
    {
        // If CAPTCHA is disabled, skip validation
        if (!_settings.Enabled)
        {
            _logger.LogDebug("CAPTCHA disabled, skipping validation for action {Action}", action);
            return;
        }

        // Token is required
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("CAPTCHA token required but not provided for action {Action}", action);
            throw new CaptchaRequiredException();
        }

        await ValidateTokenAsync(token, action, remoteIp, cancellationToken);
    }

    public async Task ValidateConditionalAsync(
        string? token,
        bool isRequired,
        string action,
        string? remoteIp = null,
        CancellationToken cancellationToken = default)
    {
        // If CAPTCHA is disabled globally, skip validation
        if (!_settings.Enabled)
        {
            _logger.LogDebug("CAPTCHA disabled, skipping validation for action {Action}", action);
            return;
        }

        // If not required for this request, skip validation
        if (!isRequired)
        {
            _logger.LogDebug("CAPTCHA not required for action {Action}", action);
            return;
        }

        // Token is required when CAPTCHA is triggered
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("CAPTCHA token required but not provided for action {Action}", action);
            throw new CaptchaRequiredException();
        }

        await ValidateTokenAsync(token, action, remoteIp, cancellationToken);
    }

    public async Task<bool> IsRequiredForLoginAsync(
        string email,
        int? riskScore = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return false;
        }

        // Check if risk score exceeds threshold
        if (riskScore.HasValue && riskScore.Value >= _settings.SmartTrigger.LoginRiskScoreThreshold)
        {
            _logger.LogDebug(
                "CAPTCHA required for login: risk score {RiskScore} >= threshold {Threshold}",
                riskScore.Value,
                _settings.SmartTrigger.LoginRiskScoreThreshold);
            return true;
        }

        // Check failed attempts count
        var normalizedEmail = email.ToLowerInvariant();
        var key = $"{LoginAttemptsKeyPrefix}{normalizedEmail}";
        var attempts = await _cache.GetIntegerAsync(key, cancellationToken) ?? 0;

        if (attempts >= _settings.SmartTrigger.LoginFailedAttemptsThreshold)
        {
            _logger.LogDebug(
                "CAPTCHA required for login: {Attempts} attempts >= threshold {Threshold}",
                attempts,
                _settings.SmartTrigger.LoginFailedAttemptsThreshold);
            return true;
        }

        return false;
    }

    public async Task<bool> IsRequiredForDeviceApprovalAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return false;
        }

        // Check failed attempts count using device ID from Redis
        var key = $"{DeviceApprovalAttemptsKeyPrefix}{deviceId}";
        var attempts = await _cache.GetIntegerAsync(key, cancellationToken) ?? 0;

        if (attempts >= _settings.SmartTrigger.DeviceApprovalFailedAttemptsThreshold)
        {
            _logger.LogDebug(
                "CAPTCHA required for device approval: {Attempts} attempts >= threshold {Threshold}",
                attempts,
                _settings.SmartTrigger.DeviceApprovalFailedAttemptsThreshold);
            return true;
        }

        return false;
    }

    public async Task<bool> IsRequiredForMfaVerifyAsync(
        string mfaToken,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return false;
        }

        // Use a hash of the MFA token as the key to avoid storing full token
        var tokenHash = GetTokenHash(mfaToken);
        var key = $"{MfaVerifyAttemptsKeyPrefix}{tokenHash}";
        var attempts = await _cache.GetIntegerAsync(key, cancellationToken) ?? 0;

        if (attempts >= _settings.SmartTrigger.MfaVerifyFailedAttemptsThreshold)
        {
            _logger.LogDebug(
                "CAPTCHA required for MFA verify: {Attempts} attempts >= threshold {Threshold}",
                attempts,
                _settings.SmartTrigger.MfaVerifyFailedAttemptsThreshold);
            return true;
        }

        return false;
    }

    public CaptchaPublicConfig GetPublicConfig()
    {
        if (!_settings.Enabled)
        {
            return CaptchaPublicConfig.Disabled();
        }

        var siteKey = _settings.Provider.ToLowerInvariant() switch
        {
            "turnstile" => _settings.Turnstile.SiteKey,
            "recaptcha" => _settings.Recaptcha.SiteKey,
            "hcaptcha" => _settings.HCaptcha.SiteKey,
            _ => string.Empty
        };

        return new CaptchaPublicConfig
        {
            Enabled = true,
            Provider = _settings.Provider.ToLowerInvariant(),
            SiteKey = siteKey
        };
    }

    private async Task ValidateTokenAsync(
        string token,
        string action,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        var result = await _provider.ValidateAsync(token, remoteIp, action, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning(
                "CAPTCHA validation failed for action {Action}: {ErrorCode}",
                action,
                result.ErrorCode);

            if (result.ErrorCode?.Contains("expired") == true)
            {
                throw new CaptchaExpiredException();
            }

            throw new CaptchaInvalidException();
        }

        _logger.LogDebug("CAPTCHA validation successful for action {Action}", action);
    }

    private static string GetTokenHash(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash)[..16]; // First 16 chars of hash
    }
}
