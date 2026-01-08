using System.Reflection;
using System.Text.Json;
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

    public static void SetCreatedAt<T>(T entity, DateTime createdAt) where T : class
    {
        var property = typeof(T).BaseType?.GetProperty("CreatedAt");
        property?.SetValue(entity, createdAt);
    }

    public static void SetUpdatedAt<T>(T entity, DateTime? updatedAt) where T : class
    {
        var property = typeof(T).BaseType?.GetProperty("UpdatedAt");
        property?.SetValue(entity, updatedAt);
    }

    // Email Provider
    public static EmailProvider CreateEmailProvider(
        string name = "Test SMTP",
        EmailProviderType type = EmailProviderType.Smtp,
        int priority = 1,
        string configurationEncrypted = "encrypted-config",
        bool isEnabled = true)
    {
        return EmailProvider.Create(name, type, priority, configurationEncrypted, isEnabled);
    }

    public static EmailProvider CreateEmailProviderWithId(
        Guid id,
        string name = "Test SMTP",
        EmailProviderType type = EmailProviderType.Smtp,
        int priority = 1,
        bool isEnabled = true)
    {
        var provider = CreateEmailProvider(name, type, priority, "encrypted-config", isEnabled);
        SetEntityId(provider, id);
        return provider;
    }

    // Email Configuration
    public static EmailConfiguration CreateEmailConfiguration()
    {
        return EmailConfiguration.CreateDefault();
    }

    public static EmailConfiguration CreateEmailConfigurationWithId(Guid id)
    {
        var config = CreateEmailConfiguration();
        SetEntityId(config, id);
        return config;
    }

    // Email Log
    public static EmailLog CreateEmailLog(
        string recipientEmail = "test@example.com",
        string subject = "Test Subject",
        string templateName = "test-template",
        string language = "en-US",
        EmailStatus status = EmailStatus.Sent,
        Guid? recipientUserId = null,
        Guid? sentViaProviderId = null,
        Guid? announcementId = null)
    {
        var log = EmailLog.Create(
            recipientEmail,
            subject,
            templateName,
            language,
            recipientUserId,
            null, // templateVariables
            announcementId);

        if (status == EmailStatus.Sent && sentViaProviderId.HasValue)
        {
            log.MarkSent(sentViaProviderId.Value);
        }
        else if (status == EmailStatus.Failed)
        {
            log.MarkFailed("Test error");
        }
        else if (status == EmailStatus.InDlq)
        {
            log.MoveToDlq("All providers failed");
        }

        return log;
    }

    public static EmailLog CreateEmailLogWithId(
        Guid id,
        string recipientEmail = "test@example.com",
        EmailStatus status = EmailStatus.Sent,
        Guid? sentViaProviderId = null)
    {
        var log = CreateEmailLog(recipientEmail, status: status, sentViaProviderId: sentViaProviderId);
        SetEntityId(log, id);
        return log;
    }

    // Email Announcement
    public static EmailAnnouncement CreateEmailAnnouncement(
        string subject = "Test Announcement",
        string htmlBody = "<p>Test content</p>",
        string? plainTextBody = "Test content",
        EmailAnnouncementTarget targetType = EmailAnnouncementTarget.AllUsers,
        string? targetPermission = null,
        List<Guid>? targetUserIds = null,
        Guid? createdByUserId = null)
    {
        var userId = createdByUserId ?? Guid.NewGuid();
        
        return targetType switch
        {
            EmailAnnouncementTarget.ByPermission when !string.IsNullOrEmpty(targetPermission) =>
                EmailAnnouncement.CreateForPermission(subject, htmlBody, targetPermission, userId, plainTextBody),
            EmailAnnouncementTarget.SelectedUsers when targetUserIds != null =>
                EmailAnnouncement.CreateForSelectedUsers(subject, htmlBody, JsonSerializer.Serialize(targetUserIds), userId, plainTextBody),
            _ => EmailAnnouncement.CreateForAllUsers(subject, htmlBody, userId, plainTextBody)
        };
    }

    public static EmailAnnouncement CreateEmailAnnouncementWithId(
        Guid id,
        string subject = "Test Announcement",
        EmailAnnouncementTarget targetType = EmailAnnouncementTarget.AllUsers,
        Guid? createdByUserId = null)
    {
        var announcement = CreateEmailAnnouncement(subject, targetType: targetType, createdByUserId: createdByUserId);
        SetEntityId(announcement, id);
        return announcement;
    }

    // Password Reset Token
    public static PasswordResetToken CreatePasswordResetToken(
        Guid userId,
        string token = "test-token",
        string code = "ABCD-1234",
        int expirationMinutes = 60)
    {
        return PasswordResetToken.Create(userId, token, code, expirationMinutes);
    }
}
