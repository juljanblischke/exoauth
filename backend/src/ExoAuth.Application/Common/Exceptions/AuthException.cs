namespace ExoAuth.Application.Common.Exceptions;

/// <summary>
/// Base exception for authentication/authorization errors.
/// </summary>
public class AuthException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public AuthException(string errorCode, string message, int statusCode = 401)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Exception for invalid credentials.
/// </summary>
public sealed class InvalidCredentialsException : AuthException
{
    public InvalidCredentialsException()
        : base("AUTH_INVALID_CREDENTIALS", "Invalid email or password", 401)
    {
    }
}

/// <summary>
/// Exception for inactive user.
/// </summary>
public sealed class UserInactiveException : AuthException
{
    public UserInactiveException()
        : base("AUTH_USER_INACTIVE", "User account is inactive", 401)
    {
    }
}

/// <summary>
/// Exception for too many login attempts.
/// </summary>
public sealed class TooManyAttemptsException : AuthException
{
    public TooManyAttemptsException()
        : base("AUTH_TOO_MANY_ATTEMPTS", "Too many failed login attempts. Please try again later.", 429)
    {
    }
}

/// <summary>
/// Exception for registration when closed.
/// </summary>
public sealed class RegistrationClosedException : AuthException
{
    public RegistrationClosedException()
        : base("AUTH_REGISTRATION_CLOSED", "Registration is closed. Please contact an administrator.", 400)
    {
    }
}

/// <summary>
/// Exception for email already exists.
/// </summary>
public sealed class EmailExistsException : AuthException
{
    public EmailExistsException()
        : base("AUTH_EMAIL_EXISTS", "Email is already in use", 409)
    {
    }
}

/// <summary>
/// Exception for invalid refresh token.
/// </summary>
public sealed class InvalidRefreshTokenException : AuthException
{
    public InvalidRefreshTokenException()
        : base("AUTH_REFRESH_TOKEN_INVALID", "Invalid or expired refresh token", 401)
    {
    }
}

/// <summary>
/// Exception for expired invite.
/// </summary>
public sealed class InviteExpiredException : AuthException
{
    public InviteExpiredException()
        : base("AUTH_INVITE_EXPIRED", "Invitation has expired. Please request a new one.", 400)
    {
    }
}

/// <summary>
/// Exception for invalid invite.
/// </summary>
public sealed class InviteInvalidException : AuthException
{
    public InviteInvalidException()
        : base("AUTH_INVITE_INVALID", "Invalid invitation token", 400)
    {
    }
}

/// <summary>
/// Exception for invalid password reset token.
/// </summary>
public sealed class PasswordResetTokenInvalidException : AuthException
{
    public PasswordResetTokenInvalidException()
        : base("PASSWORD_RESET_TOKEN_INVALID", "Invalid or expired password reset token", 400)
    {
    }
}

/// <summary>
/// Exception for expired password reset token.
/// </summary>
public sealed class PasswordResetTokenExpiredException : AuthException
{
    public PasswordResetTokenExpiredException()
        : base("PASSWORD_RESET_TOKEN_EXPIRED", "Password reset token has expired", 400)
    {
    }
}

/// <summary>
/// Exception for already used password reset token.
/// </summary>
public sealed class PasswordResetTokenUsedException : AuthException
{
    public PasswordResetTokenUsedException()
        : base("PASSWORD_RESET_TOKEN_USED", "Password reset token has already been used", 400)
    {
    }
}

/// <summary>
/// Exception for unauthorized access.
/// </summary>
public sealed class UnauthorizedException : AuthException
{
    public UnauthorizedException()
        : base("AUTH_UNAUTHORIZED", "Not authenticated", 401)
    {
    }
}

/// <summary>
/// Exception when session is not found.
/// </summary>
public sealed class SessionNotFoundException : AuthException
{
    public SessionNotFoundException()
        : base("SESSION_NOT_FOUND", "Session not found", 404)
    {
    }
}

/// <summary>
/// Exception when trying to revoke the current session.
/// </summary>
public sealed class CannotRevokeCurrentSessionException : AuthException
{
    public CannotRevokeCurrentSessionException()
        : base("SESSION_CANNOT_REVOKE_CURRENT", "Cannot revoke the current session", 400)
    {
    }
}

/// <summary>
/// Exception when MFA is already enabled.
/// </summary>
public sealed class MfaAlreadyEnabledException : AuthException
{
    public MfaAlreadyEnabledException()
        : base("MFA_ALREADY_ENABLED", "MFA is already enabled", 400)
    {
    }
}

/// <summary>
/// Exception when MFA is not enabled but required for operation.
/// </summary>
public sealed class MfaNotEnabledException : AuthException
{
    public MfaNotEnabledException()
        : base("MFA_NOT_ENABLED", "MFA is not enabled", 400)
    {
    }
}

/// <summary>
/// Exception when MFA code is invalid.
/// </summary>
public sealed class MfaCodeInvalidException : AuthException
{
    public MfaCodeInvalidException()
        : base("MFA_CODE_INVALID", "Invalid MFA code", 400)
    {
    }
}

/// <summary>
/// Exception when MFA backup code is invalid.
/// </summary>
public sealed class MfaBackupCodeInvalidException : AuthException
{
    public MfaBackupCodeInvalidException()
        : base("MFA_BACKUP_CODE_INVALID", "Invalid backup code", 400)
    {
    }
}

/// <summary>
/// Exception when MFA token is invalid or expired.
/// </summary>
public sealed class MfaTokenInvalidException : AuthException
{
    public MfaTokenInvalidException()
        : base("MFA_TOKEN_INVALID", "Invalid or expired MFA token", 401)
    {
    }
}

/// <summary>
/// Exception when MFA is required for login.
/// </summary>
public sealed class MfaRequiredException : AuthException
{
    public string MfaToken { get; }

    public MfaRequiredException(string mfaToken)
        : base("MFA_REQUIRED", "MFA verification required", 403)
    {
        MfaToken = mfaToken;
    }
}

/// <summary>
/// Exception when MFA setup is required (for users with system permissions).
/// </summary>
public sealed class MfaSetupRequiredException : AuthException
{
    public string SetupToken { get; }

    public MfaSetupRequiredException(string setupToken)
        : base("MFA_SETUP_REQUIRED", "MFA setup is required", 403)
    {
        SetupToken = setupToken;
    }
}

/// <summary>
/// Exception when account is locked.
/// </summary>
public sealed class AccountLockedException : AuthException
{
    public DateTime? LockedUntil { get; }

    public AccountLockedException(DateTime? lockedUntil = null)
        : base("ACCOUNT_LOCKED", lockedUntil.HasValue
            ? $"Account is locked until {lockedUntil:u}"
            : "Account is permanently locked", 423)
    {
        LockedUntil = lockedUntil;
    }
}

/// <summary>
/// Exception when trying to unlock an account that is not locked.
/// </summary>
public sealed class AccountNotLockedException : AuthException
{
    public AccountNotLockedException()
        : base("ACCOUNT_NOT_LOCKED", "Account is not locked", 400)
    {
    }
}

/// <summary>
/// Exception when user is already anonymized.
/// </summary>
public sealed class UserAlreadyAnonymizedException : AuthException
{
    public UserAlreadyAnonymizedException()
        : base("USER_ALREADY_ANONYMIZED", "User has already been anonymized", 400)
    {
    }
}

/// <summary>
/// Exception when trying to anonymize self.
/// </summary>
public sealed class CannotAnonymizeSelfException : AuthException
{
    public CannotAnonymizeSelfException()
        : base("CANNOT_ANONYMIZE_SELF", "Cannot anonymize your own account", 400)
    {
    }
}

/// <summary>
/// Exception when user must re-authenticate (force re-auth flag is set).
/// </summary>
public sealed class ForceReauthException : AuthException
{
    public ForceReauthException()
        : base("AUTH_FORCE_REAUTH", "Re-authentication required. Please login again.", 401)
    {
    }
}

/// <summary>
/// Exception when device approval token is invalid or expired.
/// </summary>
public sealed class ApprovalTokenInvalidException : AuthException
{
    public ApprovalTokenInvalidException()
        : base("APPROVAL_TOKEN_INVALID", "Invalid or expired approval token", 400)
    {
    }
}

/// <summary>
/// Exception when device approval token has expired.
/// </summary>
public sealed class ApprovalTokenExpiredException : AuthException
{
    public ApprovalTokenExpiredException()
        : base("APPROVAL_TOKEN_EXPIRED", "Approval token has expired", 400)
    {
    }
}

/// <summary>
/// Exception when device approval code is invalid.
/// </summary>
public sealed class ApprovalCodeInvalidException : AuthException
{
    public int? RemainingAttempts { get; }

    public ApprovalCodeInvalidException(int? remainingAttempts = null)
        : base("APPROVAL_CODE_INVALID", remainingAttempts.HasValue
            ? $"Invalid approval code. {remainingAttempts} attempts remaining."
            : "Invalid approval code", 400)
    {
        RemainingAttempts = remainingAttempts;
    }
}

/// <summary>
/// Exception when too many approval code attempts have been made.
/// </summary>
public sealed class ApprovalMaxAttemptsException : AuthException
{
    public ApprovalMaxAttemptsException()
        : base("APPROVAL_MAX_ATTEMPTS", "Too many failed attempts. Please request a new approval.", 429)
    {
    }
}

/// <summary>
/// Exception when device approval was denied by user.
/// </summary>
public sealed class DeviceApprovalDeniedException : AuthException
{
    public DeviceApprovalDeniedException()
        : base("DEVICE_APPROVAL_DENIED", "Device approval was denied", 403)
    {
    }
}

/// <summary>
/// Exception when passkey is not found.
/// </summary>
public sealed class PasskeyNotFoundException : AuthException
{
    public PasskeyNotFoundException()
        : base("PASSKEY_NOT_FOUND", "Passkey not found", 404)
    {
    }
}

/// <summary>
/// Exception when passkey credential is invalid.
/// </summary>
public sealed class PasskeyInvalidCredentialException : AuthException
{
    public PasskeyInvalidCredentialException()
        : base("PASSKEY_INVALID_CREDENTIAL", "Invalid passkey credential", 400)
    {
    }
}

/// <summary>
/// Exception when passkey challenge is expired.
/// </summary>
public sealed class PasskeyChallengeExpiredException : AuthException
{
    public PasskeyChallengeExpiredException()
        : base("PASSKEY_CHALLENGE_EXPIRED", "Passkey challenge has expired", 400)
    {
    }
}

/// <summary>
/// Exception when passkey challenge is invalid.
/// </summary>
public sealed class PasskeyChallengeInvalidException : AuthException
{
    public PasskeyChallengeInvalidException()
        : base("PASSKEY_CHALLENGE_INVALID", "Invalid passkey challenge", 400)
    {
    }
}

/// <summary>
/// Exception when passkey is already registered.
/// </summary>
public sealed class PasskeyAlreadyRegisteredException : AuthException
{
    public PasskeyAlreadyRegisteredException()
        : base("PASSKEY_ALREADY_REGISTERED", "This passkey is already registered", 409)
    {
    }
}

/// <summary>
/// Exception when passkey registration fails.
/// </summary>
public sealed class PasskeyRegistrationFailedException : AuthException
{
    public PasskeyRegistrationFailedException()
        : base("PASSKEY_REGISTRATION_FAILED", "Passkey registration failed", 400)
    {
    }
}

/// <summary>
/// Exception when user for passkey login is not found.
/// </summary>
public sealed class PasskeyUserNotFoundException : AuthException
{
    public PasskeyUserNotFoundException()
        : base("PASSKEY_USER_NOT_FOUND", "User for passkey login not found", 404)
    {
    }
}

/// <summary>
/// Exception when trying to delete the last passkey without a password.
/// </summary>
public sealed class PasskeyCannotDeleteLastException : AuthException
{
    public PasskeyCannotDeleteLastException()
        : base("PASSKEY_CANNOT_DELETE_LAST", "Cannot delete the last passkey. Please set a password first or add another passkey.", 400)
    {
    }
}


/// <summary>
/// Exception when CAPTCHA is required but not provided.
/// </summary>
public sealed class CaptchaRequiredException : AuthException
{
    public CaptchaRequiredException()
        : base("AUTH_CAPTCHA_REQUIRED", "CAPTCHA verification required", 400)
    {
    }
}

/// <summary>
/// Exception when CAPTCHA verification fails.
/// </summary>
public sealed class CaptchaInvalidException : AuthException
{
    public CaptchaInvalidException()
        : base("AUTH_CAPTCHA_INVALID", "CAPTCHA verification failed", 400)
    {
    }
}

/// <summary>
/// Exception when CAPTCHA token has expired.
/// </summary>
public sealed class CaptchaExpiredException : AuthException
{
    public CaptchaExpiredException()
        : base("AUTH_CAPTCHA_EXPIRED", "CAPTCHA token has expired", 400)
    {
    }
}

/// <summary>
/// Exception when magic link token is invalid.
/// </summary>
public sealed class MagicLinkTokenInvalidException : AuthException
{
    public MagicLinkTokenInvalidException()
        : base("MAGIC_LINK_TOKEN_INVALID", "Invalid or expired magic link token", 400)
    {
    }
}
