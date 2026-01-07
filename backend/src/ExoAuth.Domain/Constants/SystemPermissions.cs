namespace ExoAuth.Domain.Constants;

/// <summary>
/// Constants for all system-level permission names.
/// </summary>
public static class SystemPermissions
{
    // Users
    public const string UsersRead = "system:users:read";
    public const string UsersCreate = "system:users:create";
    public const string UsersUpdate = "system:users:update";
    public const string UsersDeactivate = "system:users:deactivate";
    public const string UsersActivate = "system:users:activate";
    public const string UsersMfaReset = "system:users:mfa:reset";
    public const string UsersUnlock = "system:users:unlock";
    public const string UsersSessionsView = "system:users:sessions:view";
    public const string UsersSessionsRevoke = "system:users:sessions:revoke";
    public const string UsersAnonymize = "system:users:anonymize";

    // Audit
    public const string AuditRead = "system:audit:read";

    // Permissions
    public const string PermissionsRead = "system:permissions:read";

    // Settings
    public const string SettingsRead = "system:settings:read";
    public const string SettingsUpdate = "system:settings:update";

    // Organizations
    public const string OrganizationsRead = "system:organizations:read";
    public const string OrganizationsCreate = "system:organizations:create";
    public const string OrganizationsUpdate = "system:organizations:update";
    public const string OrganizationsDelete = "system:organizations:delete";

    // Projects
    public const string ProjectsRead = "system:projects:read";
    public const string ProjectsCreate = "system:projects:create";
    public const string ProjectsUpdate = "system:projects:update";
    public const string ProjectsDelete = "system:projects:delete";

    // IP Restrictions
    public const string IpRestrictionsRead = "system:ip-restrictions:read";
    public const string IpRestrictionsManage = "system:ip-restrictions:manage";

    /// <summary>
    /// All system permissions for seeding and first user registration.
    /// </summary>
    public static readonly IReadOnlyList<(string Name, string Description, string Category)> All = new List<(string, string, string)>
    {
        // Users
        (UsersRead, "View system users", "Users"),
        (UsersCreate, "Invite new system users", "Users"),
        (UsersUpdate, "Edit system users & permissions", "Users"),
        (UsersDeactivate, "Deactivate system users", "Users"),
        (UsersActivate, "Activate system users", "Users"),
        (UsersMfaReset, "Reset MFA for system users", "Users"),
        (UsersUnlock, "Unlock locked system users", "Users"),
        (UsersSessionsView, "View sessions of system users", "Users"),
        (UsersSessionsRevoke, "Revoke sessions of system users", "Users"),
        (UsersAnonymize, "Anonymize system users (GDPR)", "Users"),

        // Audit
        (AuditRead, "View system audit logs", "Audit"),

        // Permissions
        (PermissionsRead, "View available permissions", "Permissions"),

        // Settings
        (SettingsRead, "View system settings", "Settings"),
        (SettingsUpdate, "Change system settings", "Settings"),

        // Organizations
        (OrganizationsRead, "View all organizations", "Organizations"),
        (OrganizationsCreate, "Create organizations", "Organizations"),
        (OrganizationsUpdate, "Edit organizations", "Organizations"),
        (OrganizationsDelete, "Delete organizations", "Organizations"),

        // Projects
        (ProjectsRead, "View system-owned projects", "Projects"),
        (ProjectsCreate, "Create system-owned projects", "Projects"),
        (ProjectsUpdate, "Edit system-owned projects", "Projects"),
        (ProjectsDelete, "Delete system-owned projects", "Projects"),

        // IP Restrictions
        (IpRestrictionsRead, "View IP whitelist and blacklist", "Security"),
        (IpRestrictionsManage, "Manage IP whitelist and blacklist", "Security"),
    };

    /// <summary>
    /// All permission names as a list.
    /// </summary>
    public static readonly IReadOnlyList<string> AllNames = All.Select(p => p.Name).ToList();
}
