namespace ExoAuth.Application.Common.Models;

/// <summary>
/// Result of a spoofing check for a trusted device.
/// Used to detect if someone is trying to impersonate a trusted device.
/// </summary>
public sealed record SpoofingCheckResult
{
    /// <summary>
    /// Whether the login attempt appears suspicious and may be a spoofing attempt.
    /// </summary>
    public bool IsSuspicious { get; init; }

    /// <summary>
    /// The risk score calculated for this attempt.
    /// </summary>
    public int RiskScore { get; init; }

    /// <summary>
    /// The list of suspicious factors detected.
    /// </summary>
    public IReadOnlyList<string> SuspiciousFactors { get; init; } = [];

    /// <summary>
    /// Creates a non-suspicious result (device appears legitimate).
    /// </summary>
    public static SpoofingCheckResult NotSuspicious() => new()
    {
        IsSuspicious = false,
        RiskScore = 0,
        SuspiciousFactors = []
    };

    /// <summary>
    /// Creates a suspicious result with the given factors.
    /// </summary>
    public static SpoofingCheckResult Suspicious(int riskScore, IEnumerable<string> factors) => new()
    {
        IsSuspicious = true,
        RiskScore = riskScore,
        SuspiciousFactors = factors.ToList().AsReadOnly()
    };
}
