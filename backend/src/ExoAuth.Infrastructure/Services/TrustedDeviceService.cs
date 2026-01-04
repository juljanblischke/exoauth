using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class TrustedDeviceService : ITrustedDeviceService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<TrustedDeviceService> _logger;

    public TrustedDeviceService(
        IAppDbContext context,
        ILogger<TrustedDeviceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TrustedDevice?> FindAsync(
        Guid userId,
        string deviceId,
        string? deviceFingerprint = null,
        CancellationToken ct = default)
    {
        // First try to match by DeviceId (primary match)
        var device = await _context.TrustedDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId, ct);

        if (device is not null)
            return device;

        // Fallback: try to match by fingerprint if provided (handles deviceId changes)
        if (!string.IsNullOrEmpty(deviceFingerprint))
        {
            device = await _context.TrustedDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceFingerprint == deviceFingerprint, ct);

            if (device is not null)
            {
                // Update DeviceId since fingerprint matched (deviceId may have changed)
                _logger.LogInformation(
                    "Trusted device matched by fingerprint, updating DeviceId from {OldDeviceId} to {NewDeviceId}",
                    device.DeviceId, deviceId);
            }
        }

        return device;
    }

    public async Task<TrustedDevice> AddAsync(
        Guid userId,
        string deviceId,
        DeviceInfo deviceInfo,
        GeoLocation geoLocation,
        string? deviceFingerprint = null,
        CancellationToken ct = default)
    {
        // Check if device already exists (shouldn't happen, but be safe)
        var existing = await FindAsync(userId, deviceId, deviceFingerprint, ct);
        if (existing is not null)
        {
            _logger.LogWarning(
                "Attempted to add already trusted device {DeviceId} for user {UserId}",
                deviceId, userId);
            return existing;
        }

        var trustedDevice = TrustedDevice.Create(
            userId,
            deviceId,
            deviceFingerprint,
            name: null,
            deviceInfo.Browser,
            deviceInfo.BrowserVersion,
            deviceInfo.OperatingSystem,
            deviceInfo.OsVersion,
            deviceInfo.DeviceType,
            geoLocation.IpAddress,
            geoLocation.Country,
            geoLocation.City);

        await _context.TrustedDevices.AddAsync(trustedDevice, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Added trusted device {DeviceId} (Id: {TrustedDeviceId}) for user {UserId}",
            deviceId, trustedDevice.Id, userId);

        return trustedDevice;
    }

    public async Task<List<TrustedDevice>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.TrustedDevices
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastUsedAt)
            .ToListAsync(ct);
    }

    public async Task<TrustedDevice?> GetByIdAsync(Guid deviceId, CancellationToken ct = default)
    {
        return await _context.TrustedDevices
            .FirstOrDefaultAsync(d => d.Id == deviceId, ct);
    }

    public async Task<bool> RemoveAsync(Guid deviceId, CancellationToken ct = default)
    {
        var device = await _context.TrustedDevices
            .FirstOrDefaultAsync(d => d.Id == deviceId, ct);

        if (device is null)
            return false;

        _context.TrustedDevices.Remove(device);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Removed trusted device {DeviceId} (Id: {TrustedDeviceId}) for user {UserId}",
            device.DeviceId, deviceId, device.UserId);

        return true;
    }

    public async Task<int> RemoveAllAsync(Guid userId, CancellationToken ct = default)
    {
        var devices = await _context.TrustedDevices
            .Where(d => d.UserId == userId)
            .ToListAsync(ct);

        if (devices.Count == 0)
            return 0;

        _context.TrustedDevices.RemoveRange(devices);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Removed all {Count} trusted devices for user {UserId}",
            devices.Count, userId);

        return devices.Count;
    }

    public async Task<bool> RenameAsync(Guid deviceId, string? name, CancellationToken ct = default)
    {
        var device = await _context.TrustedDevices
            .FirstOrDefaultAsync(d => d.Id == deviceId, ct);

        if (device is null)
            return false;

        device.SetName(name);
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Renamed trusted device {DeviceId} to '{Name}'", deviceId, name);

        return true;
    }

    public async Task RecordUsageAsync(
        Guid deviceId,
        string? ipAddress,
        string? country,
        string? city,
        CancellationToken ct = default)
    {
        var device = await _context.TrustedDevices
            .FirstOrDefaultAsync(d => d.Id == deviceId, ct);

        if (device is not null)
        {
            device.RecordUsage(ipAddress, country, city);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> HasAnyAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.TrustedDevices
            .AnyAsync(d => d.UserId == userId, ct);
    }

    public async Task<TrustedDevice?> FindBySessionAsync(DeviceSession session, CancellationToken ct = default)
    {
        return await FindAsync(session.UserId, session.DeviceId, session.DeviceFingerprint, ct);
    }
}
