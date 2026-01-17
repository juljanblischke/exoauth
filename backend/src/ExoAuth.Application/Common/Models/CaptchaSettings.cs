namespace ExoAuth.Application.Common.Models;

/// <summary>
/// Configuration settings for the CAPTCHA system.
/// </summary>
public sealed class CaptchaSettings
{
    /// <summary>
    /// Whether CAPTCHA is enabled globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The CAPTCHA provider to use (Turnstile, Recaptcha, HCaptcha, Disabled).
    /// </summary>
    public string Provider { get; set; } = "Turnstile";

    /// <summary>
    /// Cloudflare Turnstile configuration.
    /// </summary>
    public TurnstileSettings Turnstile { get; set; } = new();

    /// <summary>
    /// Google reCAPTCHA v3 configuration.
    /// </summary>
    public RecaptchaSettings Recaptcha { get; set; } = new();

    /// <summary>
    /// hCaptcha configuration.
    /// </summary>
    public HCaptchaSettings HCaptcha { get; set; } = new();

    /// <summary>
    /// Smart triggering thresholds for conditional CAPTCHA.
    /// </summary>
    public SmartTriggerSettings SmartTrigger { get; set; } = new();
}

/// <summary>
/// Cloudflare Turnstile configuration.
/// </summary>
public sealed class TurnstileSettings
{
    /// <summary>
    /// The site key for client-side widget.
    /// </summary>
    public string SiteKey { get; set; } = string.Empty;

    /// <summary>
    /// The secret key for server-side validation.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;
}

/// <summary>
/// Google reCAPTCHA v3 configuration.
/// </summary>
public sealed class RecaptchaSettings
{
    /// <summary>
    /// The site key for client-side widget.
    /// </summary>
    public string SiteKey { get; set; } = string.Empty;

    /// <summary>
    /// The secret key for server-side validation.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Minimum score threshold (0.0 - 1.0). Default is 0.5.
    /// </summary>
    public float MinScore { get; set; } = 0.5f;
}

/// <summary>
/// hCaptcha configuration.
/// </summary>
public sealed class HCaptchaSettings
{
    /// <summary>
    /// The site key for client-side widget.
    /// </summary>
    public string SiteKey { get; set; } = string.Empty;

    /// <summary>
    /// The secret key for server-side validation.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;
}

/// <summary>
/// Smart triggering thresholds for conditional CAPTCHA display.
/// </summary>
public sealed class SmartTriggerSettings
{
    /// <summary>
    /// Number of failed login attempts before CAPTCHA is required.
    /// </summary>
    public int LoginFailedAttemptsThreshold { get; set; } = 2;

    /// <summary>
    /// Risk score threshold (0-100) above which CAPTCHA is required for login.
    /// </summary>
    public int LoginRiskScoreThreshold { get; set; } = 70;

    /// <summary>
    /// Number of failed device approval code attempts before CAPTCHA is required.
    /// </summary>
    public int DeviceApprovalFailedAttemptsThreshold { get; set; } = 2;

    /// <summary>
    /// Number of failed MFA verification attempts before CAPTCHA is required.
    /// </summary>
    public int MfaVerifyFailedAttemptsThreshold { get; set; } = 2;
}
