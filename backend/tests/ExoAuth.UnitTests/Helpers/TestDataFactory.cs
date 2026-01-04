using System.Reflection;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;

namespace ExoAuth.UnitTests.Helpers;

/// <summary>
/// Factory for creating test data.
/// </summary>
public static class TestDataFactory
{
    public static SystemUser CreateSystemUser(
        string email = "test@example.com",
        string passwordHash = "hashedpassword",
        string firstName = "Test",
        string lastName = "User",
        bool isActive = true,
        bool emailVerified = true)
    {
        return SystemUser.Create(email, passwordHash, firstName, lastName, emailVerified);
    }

    public static SystemPermission CreateSystemPermission(
        string name = "system:users:read",
        string description = "Test permission",
        string category = "Test")
    {
        return SystemPermission.Create(name, description, category);
    }

    public static SystemInvite CreateSystemInvite(
        string email = "invited@example.com",
        string firstName = "Invited",
        string lastName = "User",
        List<Guid>? permissionIds = null,
        Guid? invitedBy = null,
        string? tokenHash = null)
    {
        // Generate token and hash if not provided
        var token = SystemInvite.GenerateToken();
        var hash = tokenHash ?? SystemInvite.HashToken(token);

        return SystemInvite.Create(
            email,
            firstName,
            lastName,
            permissionIds ?? new List<Guid>(),
            invitedBy ?? Guid.NewGuid(),
            hash
        );
    }

    public static RefreshToken CreateRefreshToken(
        Guid userId,
        string token = "test-refresh-token",
        int expirationDays = 30)
    {
        return RefreshToken.Create(userId, UserType.System, token, expirationDays);
    }

    public static List<string> CreateAllPermissions()
    {
        return new List<string>
        {
            "system:users:read",
            "system:users:create",
            "system:users:update",
            "system:users:delete",
            "system:audit:read",
            "system:settings:read",
            "system:settings:update"
        };
    }

    public static Device CreateDevice(
        Guid userId,
        string deviceId = "test-device-id",
        string? name = null,
        string? browser = "Chrome",
        string? browserVersion = "120.0.0.0",
        string? operatingSystem = "Windows",
        string? osVersion = "10",
        string? deviceType = "Desktop",
        string? ipAddress = "127.0.0.1",
        string? country = "Germany",
        string? countryCode = "DE",
        string? city = "Berlin")
    {
        var device = Device.CreateTrusted(
            userId,
            deviceId,
            fingerprint: "test-fingerprint",
            name: name,
            userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0",
            ipAddress: ipAddress
        );
        device.SetDeviceInfo(browser, browserVersion, operatingSystem, osVersion, deviceType);
        device.SetLocation(country, countryCode, city, 52.52, 13.405);
        return device;
    }

    public static Device CreateDeviceWithId(
        Guid id,
        Guid userId,
        string deviceId = "test-device-id",
        string? name = null,
        string? browser = "Chrome",
        string? operatingSystem = "Windows")
    {
        var device = CreateDevice(userId, deviceId, name, browser, operatingSystem: operatingSystem);
        SetEntityId(device, id);
        return device;
    }

    public static void SetEntityId<T>(T entity, Guid id) where T : class
    {
        var property = typeof(T).GetProperty("Id");
        var backingField = typeof(T).BaseType?.GetField("<Id>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Instance);
        backingField?.SetValue(entity, id);
    }
}
