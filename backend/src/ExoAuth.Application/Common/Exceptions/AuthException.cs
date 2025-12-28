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
