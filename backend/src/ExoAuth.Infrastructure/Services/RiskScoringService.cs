using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// Service for calculating login risk scores based on device, location, and behavioral patterns.
/// Uses a point-based system where higher scores indicate higher risk.
/// </summary>
public sealed class RiskScoringService : IRiskScoringService
{
    private readonly ILoginPatternService _loginPatternService;
    private readonly ILogger<RiskScoringService> _logger;

    // Score values from configuration
    private readonly int _newDeviceScore;
    private readonly int _newCountryScore;
    private readonly int _newCityScore;
    private readonly int _impossibleTravelScore;
    private readonly int _vpnProxyScore;
    private readonly int _unusualTimeScore;
    private readonly int _torExitNodeScore;
    private readonly int _differentDeviceTypeScore;
    private readonly int _trustedDeviceReduction;

    // Thresholds from configuration
    private readonly int _mediumThreshold;
    private readonly int _highThreshold;

    // Feature settings
    private readonly double _impossibleTravelSpeedKmh;
    private readonly bool _isEnabled;

    public RiskScoringService(
        ILoginPatternService loginPatternService,
        IConfiguration configuration,
        ILogger<RiskScoringService> logger)
    {
        _loginPatternService = loginPatternService;
        _logger = logger;

        // Load configuration
        var deviceTrust = configuration.GetSection("DeviceTrust");
        _isEnabled = deviceTrust.GetValue("Enabled", true);

        var scores = deviceTrust.GetSection("Scores");
        _newDeviceScore = scores.GetValue("NewDevice", 20);
        _newCountryScore = scores.GetValue("NewCountry", 40);
        _newCityScore = scores.GetValue("NewCity", 10);
        _impossibleTravelScore = scores.GetValue("ImpossibleTravel", 80);
        _vpnProxyScore = scores.GetValue("VpnProxy", 30);
        _unusualTimeScore = scores.GetValue("UnusualTime", 15);
        _torExitNodeScore = scores.GetValue("TorExitNode", 50);
        _differentDeviceTypeScore = scores.GetValue("DifferentDeviceType", 10);

        var thresholds = deviceTrust.GetSection("Thresholds");
        _mediumThreshold = thresholds.GetValue("Medium", 31);
        _highThreshold = thresholds.GetValue("High", 61);

        _trustedDeviceReduction = deviceTrust.GetValue("TrustedDeviceReduction", -30);
        _impossibleTravelSpeedKmh = deviceTrust.GetValue("ImpossibleTravelSpeedKmh", 800.0);
    }

    public async Task<RiskScore> CalculateAsync(
        Guid userId,
        DeviceInfo deviceInfo,
        GeoLocation geoLocation,
        bool isTrustedDevice,
        CancellationToken cancellationToken = default)
    {
        // If feature is disabled, always return low risk
        if (!_isEnabled)
        {
            _logger.LogDebug("Device trust feature is disabled, returning low risk");
            return RiskScore.Low();
        }

        var score = 0;
        var factors = new List<string>();

        // Get or create the user's login pattern
        var pattern = await _loginPatternService.GetOrCreatePatternAsync(userId, cancellationToken);

        // Special case: First login ever - treat as low risk (auto-trust on registration)
        if (pattern.IsFirstLogin)
        {
            _logger.LogDebug("First login for user {UserId}, returning low risk", userId);
            return RiskScore.Low();
        }

        // Check for new device (always applies if not trusted)
        _logger.LogDebug("Checking device trust: IsTrusted={IsTrusted}", isTrustedDevice);
        if (!isTrustedDevice)
        {
            score += _newDeviceScore;
            factors.Add(RiskFactors.NewDevice);
            _logger.LogDebug("New/untrusted device: +{Score} points", _newDeviceScore);
        }

        // Check for impossible travel first (it's the most severe)
        _logger.LogDebug("Checking impossible travel: LastLocation={LastCountry}/{LastCity}, CurrentLocation={Country}/{City}",
            pattern.LastCountry, pattern.LastCity, geoLocation.CountryCode, geoLocation.City);
        if (_loginPatternService.IsImpossibleTravel(pattern, geoLocation, _impossibleTravelSpeedKmh))
        {
            score += _impossibleTravelScore;
            factors.Add(RiskFactors.ImpossibleTravel);
            _logger.LogWarning("Impossible travel detected for user {UserId}: +{Score} points", userId, _impossibleTravelScore);
        }
        else
        {
            // Only check location anomalies if no impossible travel
            // Check for new country
            _logger.LogDebug("Checking country: Current={Country}, TypicalCountries={Typical}",
                geoLocation.CountryCode, pattern.TypicalCountries);
            if (!pattern.IsTypicalCountry(geoLocation.CountryCode))
            {
                score += _newCountryScore;
                factors.Add(RiskFactors.NewCountry);
                _logger.LogDebug("New country detected: +{Score} points", _newCountryScore);
            }
            // Check for new city (only if same country - otherwise new_country already covers it)
            else if (!pattern.IsTypicalCity(geoLocation.City))
            {
                score += _newCityScore;
                factors.Add(RiskFactors.NewCity);
                _logger.LogDebug("New city detected: +{Score} points", _newCityScore);
            }
        }

        // Check for unusual login time
        var currentHour = DateTime.UtcNow.Hour;
        _logger.LogDebug("Checking login time: CurrentHour={Hour}, TypicalHours={Typical}",
            currentHour, pattern.TypicalHours);
        if (!pattern.IsTypicalHour(currentHour))
        {
            score += _unusualTimeScore;
            factors.Add(RiskFactors.UnusualTime);
            _logger.LogDebug("Unusual login time: +{Score} points", _unusualTimeScore);
        }

        // Check for different device type
        _logger.LogDebug("Checking device type: Current={DeviceType}, TypicalTypes={Typical}",
            deviceInfo.DeviceType, pattern.TypicalDeviceTypes);
        if (!pattern.IsTypicalDeviceType(deviceInfo.DeviceType))
        {
            score += _differentDeviceTypeScore;
            factors.Add(RiskFactors.DifferentDeviceType);
            _logger.LogDebug("Different device type: +{Score} points", _differentDeviceTypeScore);
        }

        // Note: VPN/Proxy and Tor detection not implemented in this version
        // These would require external APIs (IPQualityScore, MaxMind minFraud, etc.)
        // Marked as Future Enhancement in task document

        // Apply trusted device reduction if applicable
        if (isTrustedDevice && score > 0)
        {
            _logger.LogDebug("Trusted device reduction: {Reduction} points", _trustedDeviceReduction);
            score += _trustedDeviceReduction; // This is negative
            factors.Add(RiskFactors.TrustedDevice);

            // Ensure score doesn't go below 0
            if (score < 0) score = 0;
        }

        // Determine risk level based on thresholds
        var level = DetermineRiskLevel(score);

        _logger.LogInformation(
            "Risk score calculated for user {UserId}: Score={Score}, Level={Level}, Factors=[{Factors}]",
            userId, score, level, string.Join(", ", factors));

        return RiskScore.Create(score, level, factors);
    }

    public bool RequiresApproval(RiskScore riskScore)
    {
        // If feature is disabled, never require approval
        if (!_isEnabled)
        {
            return false;
        }

        // Medium and High risk require approval
        return riskScore.Level == RiskLevel.Medium || riskScore.Level == RiskLevel.High;
    }

    private RiskLevel DetermineRiskLevel(int score)
    {
        if (score >= _highThreshold)
            return RiskLevel.High;

        if (score >= _mediumThreshold)
            return RiskLevel.Medium;

        return RiskLevel.Low;
    }
}
