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
        Guid? invitedBy = null)
    {
        return SystemInvite.Create(
            email,
            firstName,
            lastName,
            permissionIds ?? new List<Guid>(),
            invitedBy ?? Guid.NewGuid()
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
}
