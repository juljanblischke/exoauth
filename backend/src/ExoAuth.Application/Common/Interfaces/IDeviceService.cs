using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for managing devices (consolidated from DeviceSession, TrustedDevice, and DeviceApprovalService).
/// </summary>
public interface IDeviceService
{
    // ============ Device Lookup ============

    /// <summary>
    /// Finds a trusted device by user ID and device ID (with optional fingerprint fallback).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="fingerprint">Optional device fingerprint for additional matching.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The trusted device if found, null otherwise.</returns>
    Task<Device?> FindTrustedDeviceAsync(
        Guid userId,
        string deviceId,
        string? fingerprint = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a device by its ID.
    /// </summary>
    /// <param name="id">The device ID (primary key).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The device if found, null otherwise.</returns>
    Task<Device?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets all devices for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of devices.</returns>
    Task<List<Device>> GetAllForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all trusted devices for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of trusted devices.</returns>
    Task<List<Device>> GetTrustedDevicesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all pending devices for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of pending devices.</returns>
    Task<List<Device>> GetPendingDevicesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user has any trusted devices.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user has at least one trusted device.</returns>
    Task<bool> HasAnyTrustedDeviceAsync(Guid userId, CancellationToken ct = default);

    // ============ Device Creation ============

    /// <summary>
    /// Creates a new pending device that requires approval.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="riskScore">The calculated risk score.</param>
    /// <param name="riskFactors">List of risk factors.</param>
    /// <param name="deviceInfo">Device information.</param>
    /// <param name="geoLocation">Geographic location.</param>
    /// <param name="fingerprint">Optional device fingerprint.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created device and the plain text approval values (token, code).</returns>
    Task<PendingDeviceResult> CreatePendingDeviceAsync(
        Guid userId,
        string deviceId,
        int riskScore,
        IEnumerable<string> riskFactors,
        DeviceInfo deviceInfo,
        GeoLocation geoLocation,
        string? fingerprint = null,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new trusted device (for known devices).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="deviceInfo">Device information.</param>
    /// <param name="geoLocation">Geographic location.</param>
    /// <param name="fingerprint">Optional device fingerprint.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created trusted device.</returns>
    Task<Device> CreateTrustedDeviceAsync(
        Guid userId,
        string deviceId,
        DeviceInfo deviceInfo,
        GeoLocation geoLocation,
        string? fingerprint = null,
        CancellationToken ct = default);

    // ============ Device Approval ============

    /// <summary>
    /// Validates an approval token and returns the device if valid.
    /// </summary>
    /// <param name="token">The plain text token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The device if token is valid, null otherwise.</returns>
    Task<Device?> ValidateApprovalTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Validates an approval code for a specific approval token.
    /// Increments attempt counter on failure.
    /// </summary>
    /// <param name="approvalToken">The approval token (used to identify the device).</param>
    /// <param name="code">The plain text code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The validation result with the device if valid.</returns>
    Task<DeviceCodeValidationResult> ValidateApprovalCodeAsync(
        string approvalToken,
        string code,
        CancellationToken ct = default);

    /// <summary>
    /// Marks a device as trusted (approves the device).
    /// </summary>
    /// <param name="device">The device to approve.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkDeviceTrustedAsync(Device device, CancellationToken ct = default);

    /// <summary>
    /// Approves a pending device from an existing trusted session.
    /// </summary>
    /// <param name="pendingDeviceId">The pending device ID.</param>
    /// <param name="approvingUserId">The user ID (for verification).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The approved device if found and approved, null otherwise.</returns>
    Task<Device?> ApproveFromSessionAsync(Guid pendingDeviceId, Guid approvingUserId, CancellationToken ct = default);

    // ============ Device Management ============

    /// <summary>
    /// Records device usage (updates last used timestamp and optionally location).
    /// </summary>
    /// <param name="deviceId">The device ID (primary key).</param>
    /// <param name="ipAddress">The current IP address.</param>
    /// <param name="country">The current country.</param>
    /// <param name="city">The current city.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordUsageAsync(
        Guid deviceId,
        string? ipAddress = null,
        string? country = null,
        string? city = null,
        CancellationToken ct = default);

    /// <summary>
    /// Renames a device.
    /// </summary>
    /// <param name="deviceId">The device ID (primary key).</param>
    /// <param name="name">The new name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if renamed, false if not found.</returns>
    Task<bool> RenameAsync(Guid deviceId, string? name, CancellationToken ct = default);

    /// <summary>
    /// Revokes a device and all its associated refresh tokens.
    /// </summary>
    /// <param name="deviceId">The device ID (primary key).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if revoked, false if not found.</returns>
    Task<bool> RevokeAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>
    /// Revokes all devices for a user except the specified one.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="exceptDeviceId">The device ID to keep active (typically the current device).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of devices revoked.</returns>
    Task<int> RevokeAllExceptAsync(Guid userId, Guid? exceptDeviceId, CancellationToken ct = default);

    /// <summary>
    /// Removes all devices for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of devices removed.</returns>
    Task<int> RemoveAllAsync(Guid userId, CancellationToken ct = default);

    // ============ Utilities ============

    /// <summary>
    /// Generates a unique device ID for server-side device identification.
    /// </summary>
    /// <returns>A new device ID.</returns>
    string GenerateDeviceId();
}

/// <summary>
/// Result of creating a pending device with approval data.
/// </summary>
public sealed record PendingDeviceResult(
    Device Device,
    string ApprovalToken,
    string ApprovalCode);

/// <summary>
/// Result of validating an approval code for the new Device model.
/// </summary>
public sealed record DeviceCodeValidationResult
{
    public bool IsValid { get; init; }
    public Device? Device { get; init; }
    public string? Error { get; init; }
    public int Attempts { get; init; }
    public bool MaxAttemptsReached { get; init; }

    public static DeviceCodeValidationResult Success(Device device) => new()
    {
        IsValid = true,
        Device = device,
        Attempts = device.ApprovalAttempts
    };

    public static DeviceCodeValidationResult TokenInvalid() => new()
    {
        IsValid = false,
        Error = "APPROVAL_TOKEN_INVALID"
    };

    public static DeviceCodeValidationResult TokenExpired() => new()
    {
        IsValid = false,
        Error = "APPROVAL_TOKEN_EXPIRED"
    };

    public static DeviceCodeValidationResult CodeInvalid(int attempts, bool maxReached) => new()
    {
        IsValid = false,
        Error = maxReached ? "APPROVAL_MAX_ATTEMPTS" : "APPROVAL_CODE_INVALID",
        Attempts = attempts,
        MaxAttemptsReached = maxReached
    };
}
