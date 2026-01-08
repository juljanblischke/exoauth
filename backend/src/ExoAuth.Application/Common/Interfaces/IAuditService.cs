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
    public const string UserDeactivated = "system.user.deactivated";
    public const string UserActivated = "system.user.activated";

    // Token actions
    public const string TokenRefreshed = "system.token.refreshed";

    // Security actions
    public const string LoginBlocked = "system.login.blocked";

    // Invite actions
    public const string InviteRevoked = "system.invite.revoked";
    public const string InviteResent = "system.invite.resent";
    public const string InviteUpdated = "system.invite.updated";

    // Force re-auth actions
    public const string ForceReauthTriggered = "system.access.forced_reauth";

    // Error audit actions
    public const string AccessForbidden = "system.access.forbidden";
    public const string ErrorInternal = "system.error.internal";

    // Password reset actions
    public const string PasswordResetRequested = "system.password.reset_requested";
    public const string PasswordResetResent = "system.password.reset_resent";
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

    // MFA actions
    public const string MfaChallengeSent = "system.mfa.challenge_sent";
    public const string MfaSetupRequiredSent = "system.mfa.setup_required_sent";
    public const string MfaSetupStarted = "system.mfa.setup_started";
    public const string MfaSetupCompleted = "system.mfa.setup_completed";
    public const string MfaEnabled = "system.mfa.enabled";
    public const string MfaDisabled = "system.mfa.disabled";
    public const string MfaVerified = "system.mfa.verified";
    public const string MfaBackupCodeUsed = "system.mfa.backup_code_used";
    public const string MfaBackupCodesRegenerated = "system.mfa.backup_codes_regenerated";
    public const string MfaResetByAdmin = "system.mfa.reset_by_admin";

    // Account actions
    public const string AccountLocked = "system.account.locked";
    public const string AccountUnlocked = "system.account.unlocked";
    public const string AccountUnlockedByAdmin = "system.account.unlocked_by_admin";
    public const string UserAnonymized = "system.user.anonymized";
    public const string PreferencesUpdated = "system.user.preferences_updated";
    public const string SessionsRevokedByAdmin = "system.sessions.revoked_by_admin";
    public const string SessionRevokedByAdmin = "system.session.revoked_by_admin";

    // Device approval actions
    public const string DeviceApprovalRequired = "system.device.approval_required";
    public const string DeviceApprovalResent = "system.device.approval_resent";
    public const string DeviceApproved = "system.device.approved";
    public const string DeviceApprovedViaLink = "system.device.approved_via_link";
    public const string DeviceApprovedViaCode = "system.device.approved_via_code";
    public const string DeviceApprovedViaSession = "system.device.approved_via_session";
    public const string DeviceDenied = "system.device.denied";

    // Trusted device actions
    public const string TrustedDeviceAdded = "system.device.trusted_added";
    public const string TrustedDeviceRemoved = "system.device.trusted_removed";
    public const string TrustedDeviceRenamed = "system.device.trusted_renamed";
    public const string TrustedDeviceRemovedByAdmin = "system.device.trusted_removed_by_admin";
    public const string TrustedDevicesRemovedByAdmin = "system.device.trusted_all_removed_by_admin";

    // Device actions (consolidated)
    public const string DeviceRevoked = "system.device.revoked";
    public const string DeviceRenamed = "system.device.renamed";
    public const string DeviceApprovedFromSession = "system.device.approved_from_session";

    // Passkey actions
    public const string PasskeyRegistered = "system.passkey.registered";
    public const string PasskeyLogin = "system.passkey.login";
    public const string PasskeyLoginFailed = "system.passkey.login_failed";
    public const string PasskeyRenamed = "system.passkey.renamed";
    public const string PasskeyDeleted = "system.passkey.deleted";

    // IP restriction actions
    public const string IpWhitelisted = "system.ip.whitelisted";
    public const string IpBlacklisted = "system.ip.blacklisted";
    public const string IpRestrictionUpdated = "system.ip.restriction_updated";
    public const string IpRestrictionDeleted = "system.ip.restriction_deleted";
}
