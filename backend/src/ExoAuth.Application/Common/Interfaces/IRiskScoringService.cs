using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for calculating login risk scores based on device, location, and behavioral patterns.
/// </summary>
public interface IRiskScoringService
{
    /// <summary>
    /// Calculates the risk score for a login attempt.
    /// </summary>
    /// <param name="userId">The user attempting to login.</param>
    /// <param name="deviceInfo">Information about the device being used.</param>
    /// <param name="geoLocation">Geographic location of the login attempt.</param>
    /// <param name="isTrustedDevice">Whether this device is already trusted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The calculated risk score with factors and level.</returns>
    Task<RiskScore> CalculateAsync(
        Guid userId,
        DeviceInfo deviceInfo,
        GeoLocation geoLocation,
        bool isTrustedDevice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if device approval is required based on the risk score.
    /// </summary>
    /// <param name="riskScore">The calculated risk score.</param>
    /// <returns>True if device approval is required, false otherwise.</returns>
    bool RequiresApproval(RiskScore riskScore);

    /// <summary>
    /// Checks if a login attempt from a trusted device appears to be a spoofing attempt.
    /// This is used to detect impossible travel, unusual patterns, or other suspicious behavior
    /// on devices that are already trusted.
    /// </summary>
    /// <param name="userId">The user attempting to login.</param>
    /// <param name="trustedDevice">The trusted device being used.</param>
    /// <param name="currentLocation">Current geographic location of the login attempt.</param>
    /// <param name="currentDeviceInfo">Current device information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating if the attempt appears suspicious.</returns>
    Task<SpoofingCheckResult> CheckForSpoofingAsync(
        Guid userId,
        TrustedDevice trustedDevice,
        GeoLocation currentLocation,
        DeviceInfo currentDeviceInfo,
        CancellationToken cancellationToken = default);
}
