using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for tracking and analyzing user login patterns.
/// </summary>
public interface ILoginPatternService
{
    /// <summary>
    /// Gets or creates a login pattern for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The login pattern for the user.</returns>
    Task<LoginPattern> GetOrCreatePatternAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successful login and updates the user's pattern.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="geoLocation">Geographic location of the login.</param>
    /// <param name="deviceType">Type of device used (Desktop, Mobile, etc.).</param>
    /// <param name="ipAddress">IP address of the login.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordLoginAsync(
        Guid userId,
        GeoLocation geoLocation,
        string? deviceType,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a login attempt appears to be from an impossible travel scenario.
    /// </summary>
    /// <param name="pattern">The user's login pattern.</param>
    /// <param name="geoLocation">Geographic location of the current login attempt.</param>
    /// <param name="maxSpeedKmh">Maximum plausible travel speed in km/h.</param>
    /// <returns>True if the travel appears impossible, false otherwise.</returns>
    bool IsImpossibleTravel(LoginPattern pattern, GeoLocation geoLocation, double maxSpeedKmh = 800);
}
