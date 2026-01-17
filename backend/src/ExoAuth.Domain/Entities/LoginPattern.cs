using System.Text.Json;

namespace ExoAuth.Domain.Entities;

/// <summary>
/// Tracks a user's typical login patterns for risk-based authentication.
/// This includes typical countries, cities, hours, and device types.
/// </summary>
public sealed class LoginPattern : BaseEntity
{
    private const int MaxHistoryItems = 10; // Keep last 10 unique values

    public Guid UserId { get; private set; }
    public string TypicalCountries { get; private set; } = "[]"; // JSON array: ["DE", "AT"]
    public string TypicalCities { get; private set; } = "[]"; // JSON array: ["Berlin", "Munich"]
    public string TypicalHours { get; private set; } = "[]"; // JSON array: [9, 10, 11, ..., 18]
    public string TypicalDeviceTypes { get; private set; } = "[]"; // JSON array: ["Desktop", "Mobile"]
    public DateTime? LastLoginAt { get; private set; }
    public string? LastIpAddress { get; private set; }
    public string? LastCountry { get; private set; }
    public string? LastCity { get; private set; }
    public double? LastLatitude { get; private set; }
    public double? LastLongitude { get; private set; }

    // Navigation property
    public SystemUser? User { get; set; }

    private LoginPattern() { } // EF Core

    /// <summary>
    /// Creates a new login pattern for a user.
    /// </summary>
    public static LoginPattern Create(Guid userId)
    {
        return new LoginPattern
        {
            UserId = userId
        };
    }

    /// <summary>
    /// Records a login and updates the typical patterns.
    /// </summary>
    public void RecordLogin(
        string? country,
        string? city,
        int hour,
        string? deviceType,
        string? ipAddress,
        double? latitude,
        double? longitude)
    {
        // Update typical countries
        if (!string.IsNullOrEmpty(country))
        {
            var countries = DeserializeList<string>(TypicalCountries);
            AddToHistory(countries, country, MaxHistoryItems);
            TypicalCountries = SerializeList(countries);
        }

        // Update typical cities
        if (!string.IsNullOrEmpty(city))
        {
            var cities = DeserializeList<string>(TypicalCities);
            AddToHistory(cities, city, MaxHistoryItems);
            TypicalCities = SerializeList(cities);
        }

        // Update typical hours
        var hours = DeserializeList<int>(TypicalHours);
        AddToHistory(hours, hour, 24); // Max 24 different hours
        TypicalHours = SerializeList(hours);

        // Update typical device types
        if (!string.IsNullOrEmpty(deviceType))
        {
            var deviceTypes = DeserializeList<string>(TypicalDeviceTypes);
            AddToHistory(deviceTypes, deviceType, 5); // Max 5 device types
            TypicalDeviceTypes = SerializeList(deviceTypes);
        }

        // Update last login info
        LastLoginAt = DateTime.UtcNow;
        LastIpAddress = ipAddress;
        LastCountry = country;
        LastCity = city;
        LastLatitude = latitude;
        LastLongitude = longitude;

        SetUpdated();
    }

    /// <summary>
    /// Checks if the country is in the user's typical login countries.
    /// </summary>
    public bool IsTypicalCountry(string? country)
    {
        if (string.IsNullOrEmpty(country)) return true; // Unknown is not suspicious
        var countries = DeserializeList<string>(TypicalCountries);
        return countries.Count == 0 || countries.Contains(country, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the city is in the user's typical login cities.
    /// </summary>
    public bool IsTypicalCity(string? city)
    {
        if (string.IsNullOrEmpty(city)) return true; // Unknown is not suspicious
        var cities = DeserializeList<string>(TypicalCities);
        return cities.Count == 0 || cities.Contains(city, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the hour is in the user's typical login hours.
    /// </summary>
    public bool IsTypicalHour(int hour)
    {
        var hours = DeserializeList<int>(TypicalHours);
        if (hours.Count == 0) return true; // No history yet

        // Consider +/- 2 hours as typical (to account for slight variations)
        return hours.Any(h => Math.Abs(h - hour) <= 2 || Math.Abs(h - hour) >= 22); // Handle midnight wrap
    }

    /// <summary>
    /// Checks if the device type is in the user's typical device types.
    /// </summary>
    public bool IsTypicalDeviceType(string? deviceType)
    {
        if (string.IsNullOrEmpty(deviceType)) return true; // Unknown is not suspicious
        var deviceTypes = DeserializeList<string>(TypicalDeviceTypes);
        return deviceTypes.Count == 0 || deviceTypes.Contains(deviceType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Calculates the distance in kilometers from the last login location.
    /// Uses the Haversine formula.
    /// </summary>
    /// <returns>Distance in kilometers, or null if location data is unavailable.</returns>
    public double? CalculateDistanceKm(double? latitude, double? longitude)
    {
        if (!LastLatitude.HasValue || !LastLongitude.HasValue ||
            !latitude.HasValue || !longitude.HasValue)
        {
            return null;
        }

        return HaversineDistance(
            LastLatitude.Value, LastLongitude.Value,
            latitude.Value, longitude.Value);
    }

    /// <summary>
    /// Calculates the time elapsed since the last login.
    /// </summary>
    public TimeSpan? TimeSinceLastLogin()
    {
        return LastLoginAt.HasValue ? DateTime.UtcNow - LastLoginAt.Value : null;
    }

    /// <summary>
    /// Checks if this is the user's first login (no pattern history).
    /// </summary>
    public bool IsFirstLogin => !LastLoginAt.HasValue;

    /// <summary>
    /// Calculates distance between two coordinates using the Haversine formula.
    /// </summary>
    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static List<T> DeserializeList<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string SerializeList<T>(List<T> list)
    {
        return JsonSerializer.Serialize(list);
    }

    private static void AddToHistory<T>(List<T> list, T item, int maxItems)
    {
        // Remove if already exists (will be re-added at end)
        list.Remove(item);

        // Add to end (most recent)
        list.Add(item);

        // Trim to max size (remove oldest from start)
        while (list.Count > maxItems)
        {
            list.RemoveAt(0);
        }
    }
}
