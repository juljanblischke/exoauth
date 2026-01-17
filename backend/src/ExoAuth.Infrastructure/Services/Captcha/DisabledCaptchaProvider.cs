using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;

namespace ExoAuth.Infrastructure.Services.Captcha;

/// <summary>
/// No-op CAPTCHA provider for when CAPTCHA is disabled.
/// Used by enterprise customers with their own WAF or in development.
/// </summary>
public sealed class DisabledCaptchaProvider : ICaptchaProvider
{
    public string ProviderName => "disabled";

    public Task<CaptchaResult> ValidateAsync(
        string token,
        string? remoteIp = null,
        string? expectedAction = null,
        CancellationToken cancellationToken = default)
    {
        // Always succeed when CAPTCHA is disabled
        return Task.FromResult(CaptchaResult.Successful());
    }
}
