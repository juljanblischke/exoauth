using ExoAuth.Application.Common.Models;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for looking up geographic location from IP addresses.
/// </summary>
public interface IGeoIpService
{
    /// <summary>
    /// Looks up the geographic location for an IP address.
    /// Returns an empty location if the IP address is invalid or not found.
    /// </summary>
    /// <param name="ipAddress">The IP address to look up.</param>
    /// <returns>The geographic location, or empty if not found.</returns>
    GeoLocation GetLocation(string? ipAddress);

    /// <summary>
    /// Checks if the GeoIP service is available and the database is loaded.
    /// </summary>
    bool IsAvailable { get; }
}
