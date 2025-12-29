namespace ExoAuth.Application.Features.Auth.Models;

/// <summary>
/// Response when starting MFA setup.
/// </summary>
public sealed record MfaSetupResponse(
    string Secret,
    string QrCodeUri,
    string ManualEntryKey
);

/// <summary>
/// Response after confirming MFA setup.
/// When completing forced MFA setup (registration/login flow), includes auth tokens.
/// When enabling MFA from settings (already authenticated), only includes backup codes.
/// </summary>
public sealed record MfaConfirmResponse(
    bool Success,
    List<string> BackupCodes,
    // Auth fields - only populated for forced setup flow (registration/login)
    UserDto? User = null,
    string? AccessToken = null,
    string? RefreshToken = null,
    Guid? SessionId = null,
    string? DeviceId = null
);

/// <summary>
/// Response when MFA is required during login.
/// </summary>
public sealed record MfaRequiredResponse(
    bool MfaRequired,
    string MfaToken
);

/// <summary>
/// Response when MFA setup is required (for users with system permissions).
/// </summary>
public sealed record MfaSetupRequiredResponse(
    bool MfaSetupRequired,
    string SetupToken
);

/// <summary>
/// Response for MFA disable.
/// </summary>
public sealed record MfaDisableResponse(
    bool Success
);

/// <summary>
/// Response for backup code regeneration.
/// </summary>
public sealed record RegenerateBackupCodesResponse(
    List<string> BackupCodes
);

/// <summary>
/// Response for updating user preferences.
/// </summary>
public sealed record UpdatePreferencesResponse(
    bool Success,
    string Language
);

/// <summary>
/// Response for admin MFA reset.
/// </summary>
public sealed record ResetUserMfaResponse(
    bool Success
);

/// <summary>
/// Response for admin unlock user.
/// </summary>
public sealed record UnlockUserResponse(
    bool Success
);

/// <summary>
/// Response for admin revoke user sessions.
/// </summary>
public sealed record RevokeUserSessionsResponse(
    int RevokedCount
);

/// <summary>
/// Response for user anonymization.
/// </summary>
public sealed record AnonymizeUserResponse(
    bool Success,
    Guid UserId
);
