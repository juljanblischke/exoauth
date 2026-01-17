namespace ExoAuth.Application.Common.Models;

/// <summary>
/// Represents geographic location information from GeoIP lookup.
/// </summary>
public sealed record GeoLocation(
    string? IpAddress,
    string? Country,
    string? CountryCode,
    string? City,
    double? Latitude,
    double? Longitude
)
{
    /// <summary>
    /// Gets a display string for the location.
    /// </summary>
    public string? DisplayName
    {
        get
        {
            if (string.IsNullOrEmpty(City) && string.IsNullOrEmpty(Country))
                return null;

            if (!string.IsNullOrEmpty(City) && !string.IsNullOrEmpty(Country))
                return $"{City}, {Country}";

            return Country ?? City;
        }
    }

    /// <summary>
    /// Returns an empty location (all values null).
    /// </summary>
    public static GeoLocation Empty => new(null, null, null, null, null, null);
}
