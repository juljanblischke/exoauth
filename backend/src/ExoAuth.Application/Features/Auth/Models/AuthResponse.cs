namespace ExoAuth.Application.Features.Auth.Models;

/// <summary>
/// Response containing user info and tokens after successful authentication.
/// </summary>
public sealed record AuthResponse(
    UserDto? User,
    string? AccessToken,
    string? RefreshToken,
    Guid? SessionId = null,
    string? DeviceId = null,
    bool IsNewDevice = false,
    bool IsNewLocation = false,
    bool MfaRequired = false,
    string? MfaToken = null,
    bool MfaSetupRequired = false,
    string? SetupToken = null,
    bool DeviceApprovalRequired = false,
    string? ApprovalToken = null,
    int? RiskScore = null,
    string? RiskLevel = null,
    IReadOnlyList<string>? RiskFactors = null
)
{
    /// <summary>
    /// Creates a successful auth response with tokens.
    /// </summary>
    public static AuthResponse Success(
        UserDto user,
        string accessToken,
        string refreshToken,
        Guid? sessionId = null,
        string? deviceId = null,
        bool isNewDevice = false,
        bool isNewLocation = false) => new(
            user, accessToken, refreshToken, sessionId, deviceId, isNewDevice, isNewLocation);

    /// <summary>
    /// Creates an MFA required response.
    /// </summary>
    public static AuthResponse RequiresMfa(string mfaToken) => new(
        User: null,
        AccessToken: null,
        RefreshToken: null,
        MfaRequired: true,
        MfaToken: mfaToken);

    /// <summary>
    /// Creates an MFA setup required response (for users with system permissions).
    /// </summary>
    public static AuthResponse RequiresMfaSetup(string setupToken) => new(
        User: null,
        AccessToken: null,
        RefreshToken: null,
        MfaSetupRequired: true,
        SetupToken: setupToken);

    /// <summary>
    /// Creates a device approval required response.
    /// </summary>
    public static AuthResponse RequiresDeviceApproval(
        string approvalToken,
        Guid sessionId,
        string deviceId,
        int riskScore,
        string riskLevel,
        IReadOnlyList<string> riskFactors) => new(
            User: null,
            AccessToken: null,
            RefreshToken: null,
            SessionId: sessionId,
            DeviceId: deviceId,
            DeviceApprovalRequired: true,
            ApprovalToken: approvalToken,
            RiskScore: riskScore,
            RiskLevel: riskLevel,
            RiskFactors: riskFactors);
};

/// <summary>
/// Response for token refresh.
/// </summary>
public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    Guid? SessionId = null
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
    bool MfaEnabled,
    string PreferredLanguage,
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
