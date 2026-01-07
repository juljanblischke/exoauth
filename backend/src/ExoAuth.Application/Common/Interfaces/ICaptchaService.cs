using ExoAuth.Application.Common.Models;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for CAPTCHA validation with smart triggering logic.
/// </summary>
public interface ICaptchaService
{
    /// <summary>
    /// Validates a CAPTCHA token. Throws ApiException if invalid and CAPTCHA is enabled.
    /// For always-required endpoints (Register, ForgotPassword).
    /// </summary>
    /// <param name="token">The CAPTCHA token from the client.</param>
    /// <param name="action">The action name for logging/verification.</param>
    /// <param name="remoteIp">The client's IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ValidateRequiredAsync(
        string? token,
        string action,
        string? remoteIp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a CAPTCHA token if CAPTCHA is currently required.
    /// For conditional endpoints (Login with smart trigger).
    /// </summary>
    /// <param name="token">The CAPTCHA token from the client (may be null if not required).</param>
    /// <param name="isRequired">Whether CAPTCHA is required for this request.</param>
    /// <param name="action">The action name for logging/verification.</param>
    /// <param name="remoteIp">The client's IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ValidateConditionalAsync(
        string? token,
        bool isRequired,
        string action,
        string? remoteIp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if CAPTCHA is required for a login attempt (smart trigger).
    /// Based on failed attempts count or risk score.
    /// </summary>
    /// <param name="email">The email attempting to login.</param>
    /// <param name="riskScore">Optional risk score for the login attempt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if CAPTCHA is required, false otherwise.</returns>
    Task<bool> IsRequiredForLoginAsync(
        string email,
        int? riskScore = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if CAPTCHA is required for device approval (smart trigger).
    /// Based on failed code attempts.
    /// </summary>
    /// <param name="deviceId">The device ID being approved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if CAPTCHA is required, false otherwise.</returns>
    Task<bool> IsRequiredForDeviceApprovalAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if CAPTCHA is required for MFA verification (smart trigger).
    /// Based on failed MFA code attempts.
    /// </summary>
    /// <param name="mfaToken">The MFA token being verified.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if CAPTCHA is required, false otherwise.</returns>
    Task<bool> IsRequiredForMfaVerifyAsync(
        string mfaToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the public configuration for the frontend.
    /// </summary>
    /// <returns>Public CAPTCHA configuration.</returns>
    CaptchaPublicConfig GetPublicConfig();

    /// <summary>
    /// Checks if CAPTCHA is globally enabled.
    /// </summary>
    bool IsEnabled { get; }
}
