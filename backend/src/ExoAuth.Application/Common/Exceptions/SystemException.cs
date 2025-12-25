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
/// Exception for attempting to delete self.
/// </summary>
public sealed class CannotDeleteSelfException : SystemException
{
    public CannotDeleteSelfException()
        : base("SYSTEM_CANNOT_DELETE_SELF", "Cannot delete your own account", 400)
    {
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
