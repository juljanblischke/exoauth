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
