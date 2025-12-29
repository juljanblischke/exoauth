using ExoAuth.Api.Filters;
using ExoAuth.Application.Features.Auth.Commands.AcceptInvite;
using ExoAuth.Application.Features.Auth.Commands.ForgotPassword;
using ExoAuth.Application.Features.Auth.Commands.Login;
using ExoAuth.Application.Features.Auth.Commands.Logout;
using ExoAuth.Application.Features.Auth.Commands.MfaConfirm;
using ExoAuth.Application.Features.Auth.Commands.MfaDisable;
using ExoAuth.Application.Features.Auth.Commands.MfaSetup;
using ExoAuth.Application.Features.Auth.Commands.MfaVerify;
using ExoAuth.Application.Features.Auth.Commands.RefreshToken;
using ExoAuth.Application.Features.Auth.Commands.RegenerateBackupCodes;
using ExoAuth.Application.Features.Auth.Commands.Register;
using ExoAuth.Application.Features.Auth.Commands.ResetPassword;
using ExoAuth.Application.Features.Auth.Commands.RevokeAllSessions;
using ExoAuth.Application.Features.Auth.Commands.RevokeSession;
using ExoAuth.Application.Features.Auth.Commands.UpdatePreferences;
using ExoAuth.Application.Features.Auth.Commands.UpdateSession;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Application.Features.Auth.Queries.GetCurrentUser;
using ExoAuth.Application.Features.Auth.Queries.GetSessions;
using ExoAuth.Application.Features.SystemInvites.Models;
using ExoAuth.Application.Features.SystemInvites.Queries.ValidateInvite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExoAuth.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Register a new user. First user becomes a SystemUser with all permissions.
    /// First user must complete MFA setup before gaining access.
    /// </summary>
    [HttpPost("register")]
    [RateLimit(5)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var command = new RegisterCommand(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.OrganizationName,
            request.DeviceId,
            request.DeviceFingerprint,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            request.Language
        );

        var result = await Mediator.Send(command, ct);

        // Only set cookies when registration is complete (not during MFA setup flow)
        if (!result.MfaSetupRequired && result.AccessToken != null && result.RefreshToken != null)
        {
            SetAuthCookies(result.AccessToken, result.RefreshToken);
        }

        return ApiCreated(result);
    }

    /// <summary>
    /// Login with email and password.
    /// </summary>
    [HttpPost("login")]
    [RateLimit(5)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var command = new LoginCommand(
            request.Email,
            request.Password,
            request.DeviceId,
            request.DeviceFingerprint,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            request.RememberMe
        );

        var result = await Mediator.Send(command, ct);

        // Only set cookies when login is complete (not during MFA flow)
        if (!result.MfaRequired && !result.MfaSetupRequired)
        {
            SetAuthCookies(result.AccessToken!, result.RefreshToken!);
        }

        return ApiOk(result);
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [RateLimit(10)]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshTokenRequest? request, CancellationToken ct)
    {
        // Get refresh token from cookie or request body
        var refreshToken = request?.RefreshToken
            ?? Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return ApiBadRequest("Refresh token is required");
        }

        var command = new RefreshTokenCommand(
            refreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        var result = await Mediator.Send(command, ct);

        SetAuthCookies(result.AccessToken, result.RefreshToken);

        return ApiOk(result);
    }

    /// <summary>
    /// Logout and revoke refresh token.
    /// </summary>
    [HttpPost("logout")]
    [RateLimit(10)]
    [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(LogoutRequest? request, CancellationToken ct)
    {
        // Get refresh token from cookie or request body
        var refreshToken = request?.RefreshToken
            ?? Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            // Already logged out
            ClearAuthCookies();
            return ApiOk(new LogoutResponse(true));
        }

        var command = new LogoutCommand(refreshToken);

        var result = await Mediator.Send(command, ct);

        ClearAuthCookies();

        return ApiOk(result);
    }

    /// <summary>
    /// Get current authenticated user info.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [RateLimit]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var query = new GetCurrentUserQuery();

        var result = await Mediator.Send(query, ct);

        return ApiOk(result);
    }

    /// <summary>
    /// Update current user preferences (language, etc.).
    /// </summary>
    [HttpPatch("me/preferences")]
    [Authorize]
    [RateLimit]
    [ProducesResponseType(typeof(UpdatePreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreferences(UpdatePreferencesRequest request, CancellationToken ct)
    {
        var command = new UpdatePreferencesCommand(request.Language);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Validate an invitation token and get details.
    /// </summary>
    [HttpGet("invite")]
    [RateLimit(10)]
    [ProducesResponseType(typeof(InviteValidationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateInvite([FromQuery] string token, CancellationToken ct)
    {
        var query = new ValidateInviteQuery(token);

        var result = await Mediator.Send(query, ct);

        return ApiOk(result);
    }

    /// <summary>
    /// Accept an invitation and create account.
    /// </summary>
    [HttpPost("accept-invite")]
    [RateLimit(5)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptInvite(AcceptInviteRequest request, CancellationToken ct)
    {
        var command = new AcceptInviteCommand(
            request.Token,
            request.Password,
            request.Language,
            request.DeviceId,
            request.DeviceFingerprint,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        var result = await Mediator.Send(command, ct);

        SetAuthCookies(result.AccessToken!, result.RefreshToken!);

        return ApiOk(result);
    }

    /// <summary>
    /// Request a password reset email.
    /// </summary>
    [HttpPost("forgot-password")]
    [RateLimit(3)]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        var command = new ForgotPasswordCommand(request.Email);

        var result = await Mediator.Send(command, ct);

        return ApiOk(result);
    }

    /// <summary>
    /// Reset password using token or code.
    /// </summary>
    [HttpPost("reset-password")]
    [RateLimit(5)]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        var command = new ResetPasswordCommand(
            request.Token,
            request.Email,
            request.Code,
            request.NewPassword
        );

        var result = await Mediator.Send(command, ct);

        return ApiOk(result);
    }

    #region Session Management

    /// <summary>
    /// Get all active sessions for the current user.
    /// </summary>
    [HttpGet("sessions")]
    [Authorize]
    [RateLimit]
    [ProducesResponseType(typeof(List<DeviceSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        var query = new GetSessionsQuery();
        var result = await Mediator.Send(query, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Revoke a specific session.
    /// </summary>
    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize]
    [RateLimit]
    [ProducesResponseType(typeof(RevokeSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        var command = new RevokeSessionCommand(sessionId);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Revoke all sessions except the current one.
    /// </summary>
    [HttpDelete("sessions")]
    [Authorize]
    [RateLimit]
    [ProducesResponseType(typeof(RevokeAllSessionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken ct)
    {
        var command = new RevokeAllSessionsCommand();
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Update a session (rename or set trust status).
    /// </summary>
    [HttpPatch("sessions/{sessionId:guid}")]
    [Authorize]
    [RateLimit]
    [ProducesResponseType(typeof(DeviceSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSession(Guid sessionId, UpdateSessionRequest request, CancellationToken ct)
    {
        var command = new UpdateSessionCommand(sessionId, request.Name, request.IsTrusted);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    #endregion

    #region MFA

    /// <summary>
    /// Start MFA setup. Returns QR code and manual entry key.
    /// Supports dual-mode authentication:
    /// - JWT auth header: For users already logged in
    /// - SetupToken in body: For forced MFA setup during login flow
    /// </summary>
    [HttpPost("mfa/setup")]
    [RateLimit(3)]
    [ProducesResponseType(typeof(MfaSetupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MfaSetup(MfaSetupRequest? request, CancellationToken ct)
    {
        var command = new MfaSetupCommand(request?.SetupToken);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Confirm MFA setup with first TOTP code. Returns backup codes.
    /// Supports dual-mode authentication:
    /// - JWT auth header: For users already logged in (returns backup codes only)
    /// - SetupToken in body: For forced MFA setup during login/registration (returns tokens + backup codes)
    /// </summary>
    [HttpPost("mfa/confirm")]
    [RateLimit(5)]
    [ProducesResponseType(typeof(MfaConfirmResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MfaConfirm(MfaConfirmRequest request, CancellationToken ct)
    {
        var command = new MfaConfirmCommand(
            request.Code,
            request.SetupToken,
            request.DeviceId,
            request.DeviceFingerprint,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        var result = await Mediator.Send(command, ct);

        // Set cookies when completing forced setup flow (tokens returned)
        if (result.AccessToken != null && result.RefreshToken != null)
        {
            SetAuthCookies(result.AccessToken, result.RefreshToken);
        }

        return ApiOk(result);
    }

    /// <summary>
    /// Verify MFA code during login. Accepts TOTP code or backup code.
    /// </summary>
    [HttpPost("mfa/verify")]
    [RateLimit(5)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MfaVerify(MfaVerifyRequest request, CancellationToken ct)
    {
        var command = new MfaVerifyCommand(
            request.MfaToken,
            request.Code,
            request.DeviceId,
            request.DeviceFingerprint,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            request.RememberMe
        );

        var result = await Mediator.Send(command, ct);

        if (result.AccessToken != null && result.RefreshToken != null)
        {
            SetAuthCookies(result.AccessToken, result.RefreshToken);
        }

        return ApiOk(result);
    }

    /// <summary>
    /// Disable MFA. Requires current TOTP code.
    /// </summary>
    [HttpPost("mfa/disable")]
    [Authorize]
    [RateLimit(3)]
    [ProducesResponseType(typeof(MfaDisableResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MfaDisable(MfaDisableRequest request, CancellationToken ct)
    {
        var command = new MfaDisableCommand(request.Code);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Regenerate backup codes. Requires current TOTP code.
    /// </summary>
    [HttpPost("mfa/backup-codes")]
    [Authorize]
    [RateLimit(3)]
    [ProducesResponseType(typeof(RegenerateBackupCodesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegenerateBackupCodes(RegenerateBackupCodesRequest request, CancellationToken ct)
    {
        var command = new RegenerateBackupCodesCommand(request.Code);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    #endregion

    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        var secureCookies = _configuration.GetValue("Cookies:Secure", true);

        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secureCookies,
            SameSite = secureCookies ? SameSiteMode.Strict : SameSiteMode.Lax,
            Path = "/api",
            MaxAge = TimeSpan.FromMinutes(15)
        };

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secureCookies,
            SameSite = secureCookies ? SameSiteMode.Strict : SameSiteMode.Lax,
            Path = "/api/auth",
            MaxAge = TimeSpan.FromDays(30)
        };

        Response.Cookies.Append("access_token", accessToken, accessCookieOptions);
        Response.Cookies.Append("refresh_token", refreshToken, refreshCookieOptions);
    }

    private void ClearAuthCookies()
    {
        var secureCookies = _configuration.GetValue("Cookies:Secure", true);

        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secureCookies,
            SameSite = secureCookies ? SameSiteMode.Strict : SameSiteMode.Lax,
            Path = "/api",
            MaxAge = TimeSpan.Zero
        };

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secureCookies,
            SameSite = secureCookies ? SameSiteMode.Strict : SameSiteMode.Lax,
            Path = "/api/auth",
            MaxAge = TimeSpan.Zero
        };

        Response.Cookies.Delete("access_token", accessCookieOptions);
        Response.Cookies.Delete("refresh_token", refreshCookieOptions);
    }
}

// Request DTOs
public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? OrganizationName = null,
    string? DeviceId = null,
    string? DeviceFingerprint = null,
    string Language = "en-US"
);

public sealed record LoginRequest(
    string Email,
    string Password,
    string? DeviceId = null,
    string? DeviceFingerprint = null,
    bool RememberMe = false
);

public sealed record RefreshTokenRequest(
    string? RefreshToken = null
);

public sealed record LogoutRequest(
    string? RefreshToken = null
);

public sealed record AcceptInviteRequest(
    string Token,
    string Password,
    string Language = "en-US",
    string? DeviceId = null,
    string? DeviceFingerprint = null
);

public sealed record ForgotPasswordRequest(
    string Email
);

public sealed record ResetPasswordRequest(
    string? Token,
    string? Email,
    string? Code,
    string NewPassword
);

public sealed record UpdateSessionRequest(
    string? Name = null,
    bool? IsTrusted = null
);

public sealed record MfaSetupRequest(
    string? SetupToken = null
);

public sealed record MfaConfirmRequest(
    string Code,
    string? SetupToken = null,
    // Device info - needed for forced setup flow to create session
    string? DeviceId = null,
    string? DeviceFingerprint = null
);

public sealed record MfaVerifyRequest(
    string MfaToken,
    string Code,
    string? DeviceId = null,
    string? DeviceFingerprint = null,
    bool RememberMe = false
);

public sealed record MfaDisableRequest(
    string Code
);

public sealed record RegenerateBackupCodesRequest(
    string Code
);

public sealed record UpdatePreferencesRequest(
    string Language
);
