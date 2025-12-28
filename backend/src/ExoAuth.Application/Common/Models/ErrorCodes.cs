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

    // Invite Errors (400/404/429)
    public const string InviteNotFound = "INVITE_NOT_FOUND";
    public const string InviteAlreadyRevoked = "INVITE_ALREADY_REVOKED";
    public const string InviteAlreadyAccepted = "INVITE_ALREADY_ACCEPTED";
    public const string InviteResendCooldown = "INVITE_RESEND_COOLDOWN";
    public const string InviteExpired = "INVITE_EXPIRED";
    public const string InviteRevoked = "INVITE_REVOKED";

    // Server Errors (500)
    public const string InternalError = "INTERNAL_ERROR";
}
