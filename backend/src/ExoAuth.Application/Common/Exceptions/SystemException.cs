namespace ExoAuth.Application.Common.Exceptions;

/// <summary>
/// Base exception for system-level errors.
/// </summary>
public class SystemException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public SystemException(string errorCode, string message, int statusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Exception for system user not found.
/// </summary>
public sealed class SystemUserNotFoundException : SystemException
{
    public Guid UserId { get; }

    public SystemUserNotFoundException(Guid userId)
        : base("SYSTEM_USER_NOT_FOUND", $"System user with ID {userId} was not found", 404)
    {
        UserId = userId;
    }
}

/// <summary>
/// Exception for system permission not found.
/// </summary>
public sealed class SystemPermissionNotFoundException : SystemException
{
    public Guid PermissionId { get; }

    public SystemPermissionNotFoundException(Guid permissionId)
        : base("SYSTEM_PERMISSION_NOT_FOUND", $"Permission with ID {permissionId} was not found", 404)
    {
        PermissionId = permissionId;
    }
}

/// <summary>
/// Exception for attempting to remove the last permission holder.
/// </summary>
public sealed class LastPermissionHolderException : SystemException
{
    public string PermissionName { get; }

    public LastPermissionHolderException(string permissionName)
        : base("SYSTEM_LAST_PERMISSION_HOLDER",
            $"Cannot remove permission '{permissionName}' - user is the last one with this permission", 400)
    {
        PermissionName = permissionName;
    }
}

/// <summary>
/// Exception for attempting to delete/deactivate self.
/// </summary>
public sealed class CannotDeleteSelfException : SystemException
{
    public CannotDeleteSelfException()
        : base("SYSTEM_CANNOT_DELETE_SELF", "Cannot deactivate your own account", 400)
    {
    }
}

/// <summary>
/// Exception when user is already deactivated.
/// </summary>
public sealed class UserAlreadyDeactivatedException : SystemException
{
    public Guid UserId { get; }

    public UserAlreadyDeactivatedException(Guid userId)
        : base("SYSTEM_USER_ALREADY_DEACTIVATED", $"User with ID {userId} is already deactivated", 400)
    {
        UserId = userId;
    }
}

/// <summary>
/// Exception when user is already activated.
/// </summary>
public sealed class UserAlreadyActivatedException : SystemException
{
    public Guid UserId { get; }

    public UserAlreadyActivatedException(Guid userId)
        : base("SYSTEM_USER_ALREADY_ACTIVATED", $"User with ID {userId} is already active", 400)
    {
        UserId = userId;
    }
}

/// <summary>
/// Exception when trying to modify an anonymized user.
/// </summary>
public sealed class UserAnonymizedException : SystemException
{
    public Guid UserId { get; }

    public UserAnonymizedException(Guid userId)
        : base("SYSTEM_USER_ANONYMIZED", $"User with ID {userId} has been anonymized and cannot be modified", 400)
    {
        UserId = userId;
    }
}

/// <summary>
/// Exception for forbidden access.
/// </summary>
public sealed class SystemForbiddenException : SystemException
{
    public SystemForbiddenException(string? message = null)
        : base("SYSTEM_FORBIDDEN", message ?? "Access denied", 403)
    {
    }
}

/// <summary>
/// Generic not found exception.
/// </summary>
public sealed class NotFoundException : SystemException
{
    public NotFoundException(string errorCode, string message)
        : base(errorCode, message, 404)
    {
    }
}

/// <summary>
/// Generic business logic exception.
/// </summary>
public sealed class BusinessException : SystemException
{
    public BusinessException(string errorCode, string message, int statusCode = 400)
        : base(errorCode, message, statusCode)
    {
    }
}

/// <summary>
/// Exception when invite is not editable (not pending).
/// </summary>
public sealed class InviteNotEditableException : SystemException
{
    public Guid InviteId { get; }

    public InviteNotEditableException(Guid inviteId)
        : base("INVITE_NOT_EDITABLE", $"Invite with ID {inviteId} is not editable (only pending invites can be edited)", 400)
    {
        InviteId = inviteId;
    }
}

/// <summary>
/// Exception when session is not found or belongs to another user (used for admin actions).
/// </summary>
public sealed class UserSessionNotFoundException : SystemException
{
    public Guid SessionId { get; }
    public Guid UserId { get; }

    public UserSessionNotFoundException(Guid sessionId, Guid userId)
        : base("SESSION_NOT_FOUND", $"Session with ID {sessionId} was not found for user {userId}", 404)
    {
        SessionId = sessionId;
        UserId = userId;
    }
}


/// <summary>
/// Exception when IP restriction is not found.
/// </summary>
public sealed class IpRestrictionNotFoundException : SystemException
{
    public Guid RestrictionId { get; }

    public IpRestrictionNotFoundException(Guid restrictionId)
        : base("IP_RESTRICTION_NOT_FOUND", $"IP restriction with ID {restrictionId} was not found", 404)
    {
        RestrictionId = restrictionId;
    }
}

/// <summary>
/// Exception when IP address or CIDR format is invalid.
/// </summary>
public sealed class IpRestrictionInvalidCidrException : SystemException
{
    public string IpAddress { get; }

    public IpRestrictionInvalidCidrException(string ipAddress)
        : base("IP_RESTRICTION_INVALID_CIDR", $"Invalid IP address or CIDR notation: {ipAddress}", 400)
    {
        IpAddress = ipAddress;
    }
}

/// <summary>
/// Exception when an IP restriction already exists.
/// </summary>
public sealed class IpRestrictionAlreadyExistsException : SystemException
{
    public string IpAddress { get; }

    public IpRestrictionAlreadyExistsException(string ipAddress)
        : base("IP_RESTRICTION_ALREADY_EXISTS", $"An active restriction for IP address {ipAddress} already exists", 409)
    {
        IpAddress = ipAddress;
    }
}
