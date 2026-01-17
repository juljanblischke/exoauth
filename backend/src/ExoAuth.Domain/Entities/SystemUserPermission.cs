namespace ExoAuth.Domain.Entities;

public sealed class SystemUserPermission
{
    public Guid SystemUserId { get; private set; }
    public Guid SystemPermissionId { get; private set; }
    public DateTime GrantedAt { get; private set; }
    public Guid? GrantedBy { get; private set; }

    // Navigation properties
    public SystemUser SystemUser { get; private set; } = null!;
    public SystemPermission SystemPermission { get; private set; } = null!;

    private SystemUserPermission() { } // EF Core

    public static SystemUserPermission Create(
        Guid systemUserId,
        Guid systemPermissionId,
        Guid? grantedBy = null)
    {
        return new SystemUserPermission
        {
            SystemUserId = systemUserId,
            SystemPermissionId = systemPermissionId,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = grantedBy
        };
    }
}
