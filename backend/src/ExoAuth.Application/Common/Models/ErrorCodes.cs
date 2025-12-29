namespace ExoAuth.Application.Common.Models;

public static class ErrorCodes
{
    // Validation Errors (400)
    public const string ValidationRequired = "VALIDATION_REQUIRED";
    public const string ValidationInvalidFormat = "VALIDATION_INVALID_FORMAT";
    public const string ValidationMinLength = "VALIDATION_MIN_LENGTH";
    public const string ValidationMaxLength = "VALIDATION_MAX_LENGTH";

    // Authentication Errors (401)
    public const string AuthUnauthorized = "AUTH_UNAUTHORIZED";
    public const string AuthTokenExpired = "AUTH_TOKEN_EXPIRED";
    public const string AuthTokenInvalid = "AUTH_TOKEN_INVALID";
    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AuthUserInactive = "AUTH_USER_INACTIVE";
    public const string AuthRefreshTokenInvalid = "AUTH_REFRESH_TOKEN_INVALID";
    public const string AuthForceReauth = "AUTH_FORCE_REAUTH";
    public const string SessionRevoked = "SESSION_REVOKED";

    // Authorization Errors (403)
    public const string AuthForbidden = "AUTH_FORBIDDEN";
    public const string SystemForbidden = "SYSTEM_FORBIDDEN";

    // Auth Business Errors (400)
    public const string AuthInviteExpired = "AUTH_INVITE_EXPIRED";
    public const string AuthInviteInvalid = "AUTH_INVITE_INVALID";
    public const string AuthRegistrationClosed = "AUTH_REGISTRATION_CLOSED";
    public const string AuthPasswordTooWeak = "AUTH_PASSWORD_TOO_WEAK";

    // Password Reset Errors (400)
    public const string PasswordResetTokenInvalid = "PASSWORD_RESET_TOKEN_INVALID";
    public const string PasswordResetTokenExpired = "PASSWORD_RESET_TOKEN_EXPIRED";
    public const string PasswordResetTokenUsed = "PASSWORD_RESET_TOKEN_USED";

    // Conflict Errors (409)
    public const string AuthEmailExists = "AUTH_EMAIL_EXISTS";

    // Rate Limiting (429)
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string AuthTooManyAttempts = "AUTH_TOO_MANY_ATTEMPTS";

    // Resource Errors (404)
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string SystemUserNotFound = "SYSTEM_USER_NOT_FOUND";
    public const string SystemPermissionNotFound = "SYSTEM_PERMISSION_NOT_FOUND";

    // System Business Errors (400)
    public const string SystemLastPermissionHolder = "SYSTEM_LAST_PERMISSION_HOLDER";
    public const string SystemCannotDeleteSelf = "SYSTEM_CANNOT_DELETE_SELF";
    public const string SystemUserAlreadyDeactivated = "SYSTEM_USER_ALREADY_DEACTIVATED";
    public const string SystemUserAlreadyActivated = "SYSTEM_USER_ALREADY_ACTIVATED";
    public const string SystemUserAnonymized = "SYSTEM_USER_ANONYMIZED";

    // Invite Errors (400/404/429)
    public const string InviteNotFound = "INVITE_NOT_FOUND";
    public const string InviteAlreadyRevoked = "INVITE_ALREADY_REVOKED";
    public const string InviteAlreadyAccepted = "INVITE_ALREADY_ACCEPTED";
    public const string InviteResendCooldown = "INVITE_RESEND_COOLDOWN";
    public const string InviteExpired = "INVITE_EXPIRED";
    public const string InviteRevoked = "INVITE_REVOKED";

    // MFA Errors (400/401/403)
    public const string MfaRequired = "MFA_REQUIRED";
    public const string MfaSetupRequired = "MFA_SETUP_REQUIRED";
    public const string MfaCodeInvalid = "MFA_CODE_INVALID";
    public const string MfaAlreadyEnabled = "MFA_ALREADY_ENABLED";
    public const string MfaNotEnabled = "MFA_NOT_ENABLED";
    public const string MfaBackupCodeInvalid = "MFA_BACKUP_CODE_INVALID";
    public const string MfaTokenInvalid = "MFA_TOKEN_INVALID";

    // Session Errors (400/404)
    public const string SessionNotFound = "SESSION_NOT_FOUND";
    public const string SessionCannotRevokeCurrent = "SESSION_CANNOT_REVOKE_CURRENT";

    // Account Lockout Errors (423)
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string AccountLockedPermanent = "ACCOUNT_LOCKED_PERMANENT";
    public const string AccountNotLocked = "ACCOUNT_NOT_LOCKED";

    // User Errors (400)
    public const string UserAlreadyAnonymized = "USER_ALREADY_ANONYMIZED";
    public const string CannotAnonymizeSelf = "CANNOT_ANONYMIZE_SELF";

    // Server Errors (500)
    public const string InternalError = "INTERNAL_ERROR";
}
