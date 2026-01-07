using ExoAuth.Application.Common.Models;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Abstraction for CAPTCHA provider implementations.
/// </summary>
public interface ICaptchaProvider
{
    /// <summary>
    /// The name of the CAPTCHA provider.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Validates a CAPTCHA token with the provider.
    /// </summary>
    /// <param name="token">The CAPTCHA token from the client.</param>
    /// <param name="remoteIp">The client's IP address for validation.</param>
    /// <param name="expectedAction">Optional action name to verify (reCAPTCHA v3).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<CaptchaResult> ValidateAsync(
        string token,
        string? remoteIp = null,
        string? expectedAction = null,
        CancellationToken cancellationToken = default);
}
