using System.Security.Cryptography;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// Service for managing device sessions.
/// </summary>
public sealed class DeviceSessionService : IDeviceSessionService
{
    private readonly IAppDbContext _context;
    private readonly IGeoIpService _geoIpService;
    private readonly IDeviceDetectionService _deviceDetectionService;
    private readonly ILogger<DeviceSessionService> _logger;

    public DeviceSessionService(
        IAppDbContext context,
        IGeoIpService geoIpService,
        IDeviceDetectionService deviceDetectionService,
        ILogger<DeviceSessionService> logger)
    {
        _context = context;
        _geoIpService = geoIpService;
        _deviceDetectionService = deviceDetectionService;
        _logger = logger;
    }

    public async Task<(DeviceSession Session, bool IsNewDevice, bool IsNewLocation)> CreateOrUpdateSessionAsync(
        Guid userId,
        string deviceId,
        string? userAgent,
        string? ipAddress,
        string? deviceFingerprint = null,
        CancellationToken ct = default)
    {
        // Check if session already exists for this device
        var existingSession = await _context.DeviceSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.DeviceId == deviceId && !s.IsRevoked, ct);

        var deviceInfo = _deviceDetectionService.Parse(userAgent);
        var location = _geoIpService.GetLocation(ipAddress);

        bool isNewDevice = existingSession is null;
        bool isNewLocation = false;

        if (existingSession is not null)
        {
            // Check if location has changed significantly
            isNewLocation = HasLocationChanged(existingSession, location);

            // Update existing session
            existingSession.SetDeviceInfo(
                deviceInfo.Browser,
                deviceInfo.BrowserVersion,
                deviceInfo.OperatingSystem,
                deviceInfo.OsVersion,
                deviceInfo.DeviceType);

            existingSession.SetLocation(
                location.Country,
                location.CountryCode,
                location.City,
                location.Latitude,
                location.Longitude);

            existingSession.UpdateIpAddress(ipAddress);
            existingSession.RecordActivity();

            await _context.SaveChangesAsync(ct);

            _logger.LogDebug("Updated existing device session {SessionId} for user {UserId}", existingSession.Id, userId);

            return (existingSession, isNewDevice, isNewLocation);
        }

        // Check if this is a completely new device (no previous sessions with this fingerprint)
        if (!string.IsNullOrEmpty(deviceFingerprint))
        {
            var fingerprintMatch = await _context.DeviceSessions
                .AnyAsync(s => s.UserId == userId && s.DeviceFingerprint == deviceFingerprint && !s.IsRevoked, ct);

            if (fingerprintMatch)
            {
                isNewDevice = false;
            }
        }

        // Check if this is the user's FIRST session ever (registration)
        var hasAnySessions = await _context.DeviceSessions
            .AnyAsync(s => s.UserId == userId, ct);

        // Create new session
        var session = DeviceSession.Create(
            userId,
            deviceId,
            deviceName: null,
            deviceFingerprint,
            userAgent,
            ipAddress);

        session.SetDeviceInfo(
            deviceInfo.Browser,
            deviceInfo.BrowserVersion,
            deviceInfo.OperatingSystem,
            deviceInfo.OsVersion,
            deviceInfo.DeviceType);

        session.SetLocation(
            location.Country,
            location.CountryCode,
            location.City,
            location.Latitude,
            location.Longitude);

        // Auto-trust first device (registration) - this is the baseline trusted device/location
        if (!hasAnySessions)
        {
            session.Trust();
            _logger.LogInformation("Auto-trusted first device session for user {UserId}", userId);
        }

        await _context.DeviceSessions.AddAsync(session, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created new device session {SessionId} for user {UserId}", session.Id, userId);

        return (session, isNewDevice, isNewLocation);
    }

    public async Task<List<DeviceSession>> GetActiveSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.DeviceSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(ct);
    }

    public async Task<DeviceSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _context.DeviceSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
    }

    public async Task RecordActivityAsync(Guid sessionId, string? ipAddress, CancellationToken ct = default)
    {
        var session = await _context.DeviceSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsRevoked, ct);

        if (session is not null)
        {
            session.RecordActivity();
            session.UpdateIpAddress(ipAddress);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> RevokeSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _context.DeviceSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsRevoked, ct);

        if (session is null)
            return false;

        session.Revoke();

        // Also revoke any refresh tokens linked to this session
        var refreshTokens = await _context.RefreshTokens
            .Where(t => t.DeviceSessionId == sessionId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in refreshTokens)
        {
            token.Revoke();
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Revoked device session {SessionId} and {TokenCount} refresh tokens", sessionId, refreshTokens.Count);

        return true;
    }

    public async Task<int> RevokeAllSessionsExceptAsync(Guid userId, Guid? exceptSessionId, CancellationToken ct = default)
    {
        var sessions = await _context.DeviceSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.Id != exceptSessionId)
            .ToListAsync(ct);

        if (sessions.Count == 0)
            return 0;

        var sessionIds = sessions.Select(s => s.Id).ToList();

        // Revoke all sessions
        foreach (var session in sessions)
        {
            session.Revoke();
        }

        // Revoke all refresh tokens linked to these sessions
        var refreshTokens = await _context.RefreshTokens
            .Where(t => t.DeviceSessionId != null && sessionIds.Contains(t.DeviceSessionId.Value) && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in refreshTokens)
        {
            token.Revoke();
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Revoked {SessionCount} device sessions and {TokenCount} refresh tokens for user {UserId}",
            sessions.Count, refreshTokens.Count, userId);

        return sessions.Count;
    }

    public async Task<bool> SetTrustStatusAsync(Guid sessionId, bool trusted, CancellationToken ct = default)
    {
        var session = await _context.DeviceSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsRevoked, ct);

        if (session is null)
            return false;

        if (trusted)
            session.Trust();
        else
            session.Untrust();

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Set trust status to {Trusted} for session {SessionId}", trusted, sessionId);

        return true;
    }

    public async Task<bool> SetSessionNameAsync(Guid sessionId, string? name, CancellationToken ct = default)
    {
        var session = await _context.DeviceSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsRevoked, ct);

        if (session is null)
            return false;

        session.SetName(name);
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Set name to '{Name}' for session {SessionId}", name, sessionId);

        return true;
    }

    public string GenerateDeviceId()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static bool HasLocationChanged(DeviceSession existingSession, Application.Common.Models.GeoLocation newLocation)
    {
        // If we don't have location data, can't determine change
        if (string.IsNullOrEmpty(newLocation.Country) && string.IsNullOrEmpty(newLocation.City))
            return false;

        // Country change is always significant
        if (!string.IsNullOrEmpty(existingSession.CountryCode) &&
            !string.IsNullOrEmpty(newLocation.CountryCode) &&
            existingSession.CountryCode != newLocation.CountryCode)
        {
            return true;
        }

        // City change within same country might be normal for mobile users
        // We could add distance-based detection here using lat/long

        return false;
    }
}
