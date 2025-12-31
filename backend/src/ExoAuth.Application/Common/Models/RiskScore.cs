namespace ExoAuth.Application.Common.Models;

/// <summary>
/// Represents the risk level classification.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Low risk (0-30 points). Normal login flow.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium risk (31-60 points). Requires device approval.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High risk (61+ points). Requires device approval.
    /// </summary>
    High = 3
}

/// <summary>
/// Represents a calculated risk score for a login attempt.
/// </summary>
public sealed record RiskScore
{
    /// <summary>
    /// The total risk score.
    /// </summary>
    public int Score { get; init; }

    /// <summary>
    /// The risk level classification based on thresholds.
    /// </summary>
    public RiskLevel Level { get; init; }

    /// <summary>
    /// The list of risk factors that contributed to the score.
    /// </summary>
    public IReadOnlyList<string> Factors { get; init; } = [];

    /// <summary>
    /// Creates a RiskScore with the given values.
    /// </summary>
    public static RiskScore Create(int score, RiskLevel level, IEnumerable<string> factors)
    {
        return new RiskScore
        {
            Score = score,
            Level = level,
            Factors = factors.ToList().AsReadOnly()
        };
    }

    /// <summary>
    /// Creates a low-risk score (0 points, no factors).
    /// </summary>
    public static RiskScore Low() => Create(0, RiskLevel.Low, []);
}

/// <summary>
/// Known risk factor identifiers.
/// </summary>
public static class RiskFactors
{
    public const string NewDevice = "new_device";
    public const string NewCountry = "new_country";
    public const string NewCity = "new_city";
    public const string ImpossibleTravel = "impossible_travel";
    public const string VpnProxy = "vpn_proxy";
    public const string UnusualTime = "unusual_time";
    public const string TorExitNode = "tor_exit_node";
    public const string DifferentDeviceType = "different_device_type";
    public const string TrustedDevice = "trusted_device";
}
