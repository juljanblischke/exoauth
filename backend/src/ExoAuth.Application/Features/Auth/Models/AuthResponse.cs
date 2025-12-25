namespace ExoAuth.Application.Features.Auth.Models;

/// <summary>
/// Response containing user info and tokens after successful authentication.
/// </summary>
public sealed record AuthResponse(
    UserDto User,
    string AccessToken,
    string RefreshToken
);

/// <summary>
/// Response for token refresh.
/// </summary>
public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken
);

/// <summary>
/// User information returned in auth responses.
/// </summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    bool IsActive,
    bool EmailVerified,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    IReadOnlyList<string> Permissions
);

/// <summary>
/// Success response for logout.
/// </summary>
public sealed record LogoutResponse(
    bool Success
);
