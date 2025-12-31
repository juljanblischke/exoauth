using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// Service for tracking and analyzing user login patterns.
/// Used by RiskScoringService to determine if a login attempt is suspicious.
/// </summary>
public sealed class LoginPatternService : ILoginPatternService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<LoginPatternService> _logger;
    private readonly int _patternHistoryDays;

    public LoginPatternService(
        IAppDbContext context,
        IConfiguration configuration,
        ILogger<LoginPatternService> logger)
    {
        _context = context;
        _logger = logger;
        _patternHistoryDays = configuration.GetValue("DeviceTrust:PatternHistoryDays", 90);
    }

    public async Task<LoginPattern> GetOrCreatePatternAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var pattern = await _context.LoginPatterns
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (pattern is not null)
        {
            return pattern;
        }

        // Create new pattern for user
        pattern = LoginPattern.Create(userId);
        await _context.LoginPatterns.AddAsync(pattern, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Created new login pattern for user {UserId}", userId);
        return pattern;
    }

    public async Task RecordLoginAsync(
        Guid userId,
        GeoLocation geoLocation,
        string? deviceType,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var pattern = await GetOrCreatePatternAsync(userId, cancellationToken);

        var currentHour = DateTime.UtcNow.Hour;

        pattern.RecordLogin(
            country: geoLocation.CountryCode,
            city: geoLocation.City,
            hour: currentHour,
            deviceType: deviceType,
            ipAddress: ipAddress,
            latitude: geoLocation.Latitude,
            longitude: geoLocation.Longitude);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Recorded login for user {UserId}: Country={Country}, City={City}, Hour={Hour}, DeviceType={DeviceType}",
            userId, geoLocation.CountryCode, geoLocation.City, currentHour, deviceType);
    }

    public bool IsImpossibleTravel(LoginPattern pattern, GeoLocation geoLocation, double maxSpeedKmh = 800)
    {
        // No previous login - can't be impossible travel
        if (pattern.IsFirstLogin || !pattern.LastLoginAt.HasValue)
        {
            return false;
        }

        // Calculate distance from last login
        var distanceKm = pattern.CalculateDistanceKm(geoLocation.Latitude, geoLocation.Longitude);

        // If we can't calculate distance (missing coordinates), assume not impossible travel
        if (!distanceKm.HasValue)
        {
            return false;
        }

        // Calculate time since last login
        var timeSinceLastLogin = pattern.TimeSinceLastLogin();
        if (!timeSinceLastLogin.HasValue || timeSinceLastLogin.Value.TotalHours <= 0)
        {
            return false;
        }

        // Calculate required speed to travel that distance
        var requiredSpeedKmh = distanceKm.Value / timeSinceLastLogin.Value.TotalHours;

        // If required speed exceeds max plausible speed, it's impossible travel
        var isImpossible = requiredSpeedKmh > maxSpeedKmh;

        if (isImpossible)
        {
            _logger.LogWarning(
                "Impossible travel detected for user {UserId}: Distance={Distance:F0}km in {Time:F1}h = {Speed:F0}km/h (max {MaxSpeed}km/h)",
                pattern.UserId, distanceKm.Value, timeSinceLastLogin.Value.TotalHours, requiredSpeedKmh, maxSpeedKmh);
        }

        return isImpossible;
    }
}
