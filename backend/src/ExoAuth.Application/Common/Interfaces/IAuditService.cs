namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for logging audit events.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs a system audit event.
    /// </summary>
    /// <param name="action">The action performed.</param>
    /// <param name="userId">The user who performed the action (null for system/anonymous).</param>
    /// <param name="targetUserId">The user who was affected by the action.</param>
    /// <param name="entityType">The type of entity affected.</param>
    /// <param name="entityId">The ID of the entity affected.</param>
    /// <param name="details">Additional details about the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogAsync(
        string action,
        Guid? userId = null,
        Guid? targetUserId = null,
        string? entityType = null,
        Guid? entityId = null,
        object? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a system audit event with request context (IP, User-Agent).
    /// </summary>
    Task LogWithContextAsync(
        string action,
        Guid? userId = null,
        Guid? targetUserId = null,
        string? entityType = null,
        Guid? entityId = null,
        object? details = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Standard audit action names for system events.
/// </summary>
public static class AuditActions
{
    // User actions
    public const string UserRegistered = "system.user.registered";
    public const string UserLogin = "system.user.login";
    public const string UserLoginFailed = "system.user.login.failed";
    public const string UserLogout = "system.user.logout";
    public const string UserInvited = "system.user.invited";
    public const string UserInviteAccepted = "system.user.invite.accepted";
    public const string UserUpdated = "system.user.updated";
    public const string UserPermissionsUpdated = "system.user.permissions.updated";
    public const string UserDeleted = "system.user.deleted";

    // Token actions
    public const string TokenRefreshed = "system.token.refreshed";

    // Security actions
    public const string LoginBlocked = "system.login.blocked";

    // Invite actions
    public const string InviteRevoked = "system.invite.revoked";
    public const string InviteResent = "system.invite.resent";

    // Force re-auth actions
    public const string ForceReauthTriggered = "system.access.forced_reauth";

    // Error audit actions
    public const string AccessForbidden = "system.access.forbidden";
    public const string ErrorInternal = "system.error.internal";

    // Password reset actions
    public const string PasswordResetRequested = "system.password.reset_requested";
    public const string PasswordResetCompleted = "system.password.reset_completed";
    public const string PasswordChanged = "system.password.changed";

    // Session actions
    public const string SessionCreated = "system.session.created";
    public const string SessionRevoked = "system.session.revoked";
    public const string SessionRevokedAll = "system.session.revoked_all";
    public const string SessionTrusted = "system.session.trusted";
    public const string SessionRenamed = "system.session.renamed";
    public const string LoginNewDevice = "system.login.new_device";
    public const string LoginNewLocation = "system.login.new_location";
}
