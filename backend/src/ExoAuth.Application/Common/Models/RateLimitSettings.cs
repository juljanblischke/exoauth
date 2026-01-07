namespace ExoAuth.Application.Common.Models;

/// <summary>
/// Configuration settings for the advanced rate limiting system.
/// </summary>
public sealed class RateLimitSettings
{
    /// <summary>
    /// Whether rate limiting is enabled globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Named presets with per-minute and per-hour limits.
    /// </summary>
    public Dictionary<string, RateLimitPreset> Presets { get; set; } = new()
    {
        ["login"] = new RateLimitPreset { PerMinute = 5, PerHour = 30 },
        ["register"] = new RateLimitPreset { PerMinute = 3, PerHour = 20 },
        ["forgot-password"] = new RateLimitPreset { PerMinute = 3, PerHour = 10 },
        ["mfa"] = new RateLimitPreset { PerMinute = 10, PerHour = 60 },
        ["sensitive"] = new RateLimitPreset { PerMinute = 20, PerHour = 200 },
        ["default"] = new RateLimitPreset { PerMinute = 100, PerHour = 1000 },
        ["relaxed"] = new RateLimitPreset { PerMinute = 500, PerHour = 5000 }
    };

    /// <summary>
    /// Auto-blacklist configuration for repeated violations.
    /// </summary>
    public AutoBlacklistSettings AutoBlacklist { get; set; } = new();
}

/// <summary>
/// Rate limit preset with multiple time windows.
/// </summary>
public sealed class RateLimitPreset
{
    /// <summary>
    /// Maximum requests allowed per minute.
    /// </summary>
    public int PerMinute { get; set; }

    /// <summary>
    /// Maximum requests allowed per hour.
    /// </summary>
    public int PerHour { get; set; }
}

/// <summary>
/// Configuration for automatic IP blacklisting on repeated violations.
/// </summary>
public sealed class AutoBlacklistSettings
{
    /// <summary>
    /// Whether auto-blacklisting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Number of rate limit violations before auto-blacklisting.
    /// </summary>
    public int ViolationThreshold { get; set; } = 10;

    /// <summary>
    /// Time window in minutes to count violations.
    /// </summary>
    public int WithinMinutes { get; set; } = 5;

    /// <summary>
    /// Duration in minutes to block the IP after auto-blacklisting.
    /// </summary>
    public int BlockDurationMinutes { get; set; } = 60;
}
