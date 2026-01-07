namespace ExoAuth.Application.Common.Models;

/// <summary>
/// Result of a CAPTCHA validation attempt.
/// </summary>
public sealed record CaptchaResult
{
    /// <summary>
    /// Whether the CAPTCHA validation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The score returned by the provider (reCAPTCHA v3: 0.0 - 1.0).
    /// Null for providers that don't return scores.
    /// </summary>
    public float? Score { get; init; }

    /// <summary>
    /// The action name if provided by the validation request.
    /// </summary>
    public string? Action { get; init; }

    /// <summary>
    /// Error code if the validation failed.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Creates a successful CAPTCHA result.
    /// </summary>
    public static CaptchaResult Successful(float? score = null, string? action = null)
    {
        return new CaptchaResult
        {
            Success = true,
            Score = score,
            Action = action
        };
    }

    /// <summary>
    /// Creates a failed CAPTCHA result.
    /// </summary>
    public static CaptchaResult Failed(string errorCode)
    {
        return new CaptchaResult
        {
            Success = false,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Public configuration exposed to the frontend.
/// </summary>
public sealed record CaptchaPublicConfig
{
    /// <summary>
    /// Whether CAPTCHA is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// The CAPTCHA provider name (turnstile, recaptcha, hcaptcha).
    /// </summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>
    /// The site key for the client-side widget.
    /// </summary>
    public string SiteKey { get; init; } = string.Empty;

    /// <summary>
    /// Creates a disabled CAPTCHA configuration.
    /// </summary>
    public static CaptchaPublicConfig Disabled()
    {
        return new CaptchaPublicConfig
        {
            Enabled = false,
            Provider = "disabled",
            SiteKey = string.Empty
        };
    }
}
