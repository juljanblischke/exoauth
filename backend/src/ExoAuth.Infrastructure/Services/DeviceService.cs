using System.Security.Cryptography;
using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// Consolidated device service that replaces DeviceSessionService, TrustedDeviceService, and DeviceApprovalService.
/// </summary>
public sealed class DeviceService : IDeviceService
{
    private readonly IAppDbContext _context;
    private readonly IGeoIpService _geoIpService;
    private readonly IDeviceDetectionService _deviceDetectionService;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly ILogger<DeviceService> _logger;
    private readonly int _expirationMinutes;
    private readonly int _maxCodeAttempts;
    private const int MaxRetries = 3;

    public DeviceService(
        IAppDbContext context,
        IGeoIpService geoIpService,
        IDeviceDetectionService deviceDetectionService,
        IRevokedSessionService revokedSessionService,
        IConfiguration configuration,
        ILogger<DeviceService> logger)
    {
        _context = context;
        _geoIpService = geoIpService;
        _deviceDetectionService = deviceDetectionService;
        _revokedSessionService = revokedSessionService;
        _logger = logger;

        var deviceTrust = configuration.GetSection("DeviceTrust");
        _expirationMinutes = deviceTrust.GetValue("ApprovalExpiryMinutes", 30);
        _maxCodeAttempts = deviceTrust.GetValue("MaxCodeAttempts", 3);
    }

    // ============ Device Lookup ============

    public async Task<Device?> FindTrustedDeviceAsync(
        Guid userId,
        string deviceId,
        string? fingerprint = null,
        CancellationToken ct = default)
    {
        // First try to match by DeviceId (primary match)
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId && d.Status == DeviceStatus.Trusted, ct);

        if (device is not null)
            return device;

        // Fallback: try to match by fingerprint if provided (handles deviceId changes)
        if (!string.IsNullOrEmpty(fingerprint))
        {
            device = await _context.Devices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.Fingerprint == fingerprint && d.Status == DeviceStatus.Trusted, ct);

            if (device is not null)
            {
                _logger.LogInformation(
                    "Trusted device matched by fingerprint, DeviceId may have changed from {OldDeviceId} to {NewDeviceId}",
                    device.DeviceId, deviceId);
            }
        }

        return device;
    }

    public async Task<Device?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<List<Device>> GetAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Devices
            .Where(d => d.UserId == userId && d.Status != DeviceStatus.Revoked)
            .OrderByDescending(d => d.LastUsedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Device>> GetTrustedDevicesAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Devices
            .Where(d => d.UserId == userId && d.Status == DeviceStatus.Trusted)
            .OrderByDescending(d => d.LastUsedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Device>> GetPendingDevicesAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Devices
            .Where(d => d.UserId == userId && d.Status == DeviceStatus.PendingApproval)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> HasAnyTrustedDeviceAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Devices
            .AnyAsync(d => d.UserId == userId && d.Status == DeviceStatus.Trusted, ct);
    }

    // ============ Device Creation ============

    public async Task<PendingDeviceResult> CreatePendingDeviceAsync(
        Guid userId,
        string deviceId,
        int riskScore,
        IEnumerable<string> riskFactors,
        DeviceInfo deviceInfo,
        GeoLocation geoLocation,
        string? fingerprint = null,
        CancellationToken ct = default)
    {
        // Serialize risk factors to JSON
        var riskFactorsJson = JsonSerializer.Serialize(riskFactors.ToList());

        // Check if any device already exists for this user + deviceId (regardless of status)
        var existingDevice = await _context.Devices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId, ct);

        if (existingDevice is not null)
        {
            // Reuse existing device - reset to pending with new approval credentials
            for (var attempt = 0; attempt < MaxRetries; attempt++)
            {
                var token = Device.GenerateApprovalToken();
                var code = Device.GenerateApprovalCode();

                // Check for token hash collision
                var tokenHash = Device.HashForCheck(token);
                var exists = await _context.Devices
                    .AnyAsync(x => x.ApprovalTokenHash == tokenHash && x.Id != existingDevice.Id, ct);

                if (exists)
                {
                    _logger.LogWarning("Device approval token collision detected on attempt {Attempt}, regenerating", attempt + 1);
                    continue;
                }

                existingDevice.ResetToPending(token, code, riskScore, riskFactorsJson, _expirationMinutes);

                // Update device info and location
                existingDevice.SetDeviceInfo(
                    deviceInfo.Browser,
                    deviceInfo.BrowserVersion,
                    deviceInfo.OperatingSystem,
                    deviceInfo.OsVersion,
                    deviceInfo.DeviceType);

                existingDevice.SetLocation(
                    geoLocation.Country,
                    geoLocation.CountryCode,
                    geoLocation.City,
                    geoLocation.Latitude,
                    geoLocation.Longitude);

                if (!string.IsNullOrEmpty(fingerprint))
                {
                    existingDevice.UpdateFingerprint(fingerprint);
                }

                existingDevice.UpdateIpAddress(geoLocation.IpAddress);

                await _context.SaveChangesAsync(ct);

                // Clear any revoked session status in Redis (important for re-login after admin revocation)
                await _revokedSessionService.ClearRevokedSessionAsync(existingDevice.Id, ct);

                _logger.LogInformation(
                    "Reset existing device {DeviceId} (Id: {DeviceDbId}) to pending for user {UserId}, risk score {RiskScore}",
                    deviceId, existingDevice.Id, userId, riskScore);

                return new PendingDeviceResult(existingDevice, token, code);
            }

            throw new InvalidOperationException("Failed to generate unique device approval token after maximum retries");
        }

        // No existing device - create new one
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var token = Device.GenerateApprovalToken();
            var code = Device.GenerateApprovalCode();

            // Check for token hash collision (extremely unlikely but we check anyway)
            var tokenHash = Device.HashForCheck(token);
            var exists = await _context.Devices
                .AnyAsync(x => x.ApprovalTokenHash == tokenHash, ct);

            if (exists)
            {
                _logger.LogWarning("Device approval token collision detected on attempt {Attempt}, regenerating", attempt + 1);
                continue;
            }

            var device = Device.CreatePending(
                userId,
                deviceId,
                token,
                code,
                riskScore,
                riskFactorsJson,
                fingerprint,
                name: null,
                null, // UserAgent not available through DeviceInfo
                geoLocation.IpAddress,
                _expirationMinutes);

            device.SetDeviceInfo(
                deviceInfo.Browser,
                deviceInfo.BrowserVersion,
                deviceInfo.OperatingSystem,
                deviceInfo.OsVersion,
                deviceInfo.DeviceType);

            device.SetLocation(
                geoLocation.Country,
                geoLocation.CountryCode,
                geoLocation.City,
                geoLocation.Latitude,
                geoLocation.Longitude);

            await _context.Devices.AddAsync(device, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Created pending device {DeviceId} (Id: {DeviceDbId}) for user {UserId}, risk score {RiskScore}",
                deviceId, device.Id, userId, riskScore);

            return new PendingDeviceResult(device, token, code);
        }

        // This should never happen given the entropy of our tokens
        throw new InvalidOperationException("Failed to generate unique device approval token after maximum retries");
    }

    public async Task<Device> CreateTrustedDeviceAsync(
        Guid userId,
        string deviceId,
        DeviceInfo deviceInfo,
        GeoLocation geoLocation,
        string? fingerprint = null,
        CancellationToken ct = default)
    {
        // Check if device already exists (shouldn't happen, but be safe)
        var existing = await FindTrustedDeviceAsync(userId, deviceId, fingerprint, ct);
        if (existing is not null)
        {
            _logger.LogWarning(
                "Attempted to add already trusted device {DeviceId} for user {UserId}",
                deviceId, userId);
            existing.RecordUsage(geoLocation.IpAddress, geoLocation.Country, geoLocation.City);
            await _context.SaveChangesAsync(ct);
            return existing;
        }

        var device = Device.CreateTrusted(
            userId,
            deviceId,
            fingerprint,
            name: null,
            null, // UserAgent not available through DeviceInfo
            geoLocation.IpAddress);

        device.SetDeviceInfo(
            deviceInfo.Browser,
            deviceInfo.BrowserVersion,
            deviceInfo.OperatingSystem,
            deviceInfo.OsVersion,
            deviceInfo.DeviceType);

        device.SetLocation(
            geoLocation.Country,
            geoLocation.CountryCode,
            geoLocation.City,
            geoLocation.Latitude,
            geoLocation.Longitude);

        await _context.Devices.AddAsync(device, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created trusted device {DeviceId} (Id: {DeviceDbId}) for user {UserId}",
            deviceId, device.Id, userId);

        return device;
    }

    // ============ Device Approval ============

    public async Task<Device?> ValidateApprovalTokenAsync(string token, CancellationToken ct = default)
    {
        var tokenHash = Device.HashForCheck(token);

        var device = await _context.Devices
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.ApprovalTokenHash == tokenHash, ct);

        if (device is null)
        {
            _logger.LogDebug("Device approval token not found");
            return null;
        }

        // Check if expired
        if (device.IsApprovalExpired)
        {
            _logger.LogDebug("Device approval token is expired");
            // Mark as revoked if still pending
            if (device.Status == DeviceStatus.PendingApproval)
            {
                device.Revoke();
                await _context.SaveChangesAsync(ct);
            }
            return null;
        }

        // Check if not pending
        if (device.Status != DeviceStatus.PendingApproval)
        {
            _logger.LogDebug("Device is not pending approval (status: {Status})", device.Status);
            return null;
        }

        return device;
    }

    public async Task<DeviceCodeValidationResult> ValidateApprovalCodeAsync(
        string approvalToken,
        string code,
        CancellationToken ct = default)
    {
        // First validate the token to get the device
        var device = await ValidateApprovalTokenAsync(approvalToken, ct);

        if (device is null)
        {
            return DeviceCodeValidationResult.TokenInvalid();
        }

        // Check if max attempts reached
        if (device.ApprovalAttempts >= _maxCodeAttempts)
        {
            _logger.LogWarning(
                "Device approval max attempts reached for device {DeviceId}",
                device.Id);
            return DeviceCodeValidationResult.CodeInvalid(device.ApprovalAttempts, true);
        }

        // Validate the code
        if (!device.ValidateApprovalCode(code))
        {
            // Increment attempts
            var newAttemptCount = device.IncrementApprovalAttempts();
            await _context.SaveChangesAsync(ct);

            var maxReached = newAttemptCount >= _maxCodeAttempts;

            _logger.LogDebug(
                "Invalid device approval code for device {DeviceId}, attempt {Attempt}/{MaxAttempts}",
                device.Id, newAttemptCount, _maxCodeAttempts);

            return DeviceCodeValidationResult.CodeInvalid(newAttemptCount, maxReached);
        }

        _logger.LogDebug("Device approval code validated successfully for device {DeviceId}", device.Id);
        return DeviceCodeValidationResult.Success(device);
    }

    public async Task MarkDeviceTrustedAsync(Device device, CancellationToken ct = default)
    {
        device.MarkTrusted();
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Device {DeviceId} marked as trusted for user {UserId}",
            device.Id, device.UserId);
    }

    public async Task<Device?> ApproveFromSessionAsync(Guid pendingDeviceId, Guid approvingUserId, CancellationToken ct = default)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == pendingDeviceId && d.UserId == approvingUserId && d.Status == DeviceStatus.PendingApproval, ct);

        if (device is null)
        {
            _logger.LogDebug("Pending device {DeviceId} not found for user {UserId}", pendingDeviceId, approvingUserId);
            return null;
        }

        device.MarkTrusted();
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Device {DeviceId} approved from existing session for user {UserId}",
            device.Id, device.UserId);

        return device;
    }

    // ============ Device Management ============

    public async Task RecordUsageAsync(
        Guid deviceId,
        string? ipAddress = null,
        string? country = null,
        string? city = null,
        CancellationToken ct = default)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.Status == DeviceStatus.Trusted, ct);

        if (device is not null)
        {
            device.RecordUsage(ipAddress, country, city);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> RenameAsync(Guid deviceId, string? name, CancellationToken ct = default)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.Status != DeviceStatus.Revoked, ct);

        if (device is null)
            return false;

        device.SetName(name);
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Renamed device {DeviceId} to '{Name}'", deviceId, name);

        return true;
    }

    public async Task<bool> RevokeAsync(Guid deviceId, CancellationToken ct = default)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.Status != DeviceStatus.Revoked, ct);

        if (device is null)
            return false;

        device.Revoke();

        // Also revoke any refresh tokens linked to this device
        var refreshTokens = await _context.RefreshTokens
            .Where(t => t.DeviceId == deviceId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in refreshTokens)
        {
            token.Revoke();
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Revoked device {DeviceId} and {TokenCount} refresh tokens", deviceId, refreshTokens.Count);

        return true;
    }

    public async Task<int> RevokeAllExceptAsync(Guid userId, Guid? exceptDeviceId, CancellationToken ct = default)
    {
        var devices = await _context.Devices
            .Where(d => d.UserId == userId && d.Status != DeviceStatus.Revoked && d.Id != exceptDeviceId)
            .ToListAsync(ct);

        if (devices.Count == 0)
            return 0;

        var deviceIds = devices.Select(d => d.Id).ToList();

        // Revoke all devices
        foreach (var device in devices)
        {
            device.Revoke();
        }

        // Revoke all refresh tokens linked to these devices
        var refreshTokens = await _context.RefreshTokens
            .Where(t => t.DeviceId != null && deviceIds.Contains(t.DeviceId.Value) && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in refreshTokens)
        {
            token.Revoke();
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Revoked {DeviceCount} devices and {TokenCount} refresh tokens for user {UserId}",
            devices.Count, refreshTokens.Count, userId);

        return devices.Count;
    }

    public async Task<int> RemoveAllAsync(Guid userId, CancellationToken ct = default)
    {
        var devices = await _context.Devices
            .Where(d => d.UserId == userId)
            .ToListAsync(ct);

        if (devices.Count == 0)
            return 0;

        _context.Devices.RemoveRange(devices);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Removed all {Count} devices for user {UserId}",
            devices.Count, userId);

        return devices.Count;
    }

    // ============ Utilities ============

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

    // ============ Private Helpers ============


}
