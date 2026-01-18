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
using ExoAuth.Application.Features.Auth.Commands.UpdatePreferences;
using ExoAuth.Application.Features.Auth.Commands.ApproveDevice;
using ExoAuth.Application.Features.Auth.Commands.ApproveDeviceLink;
using ExoAuth.Application.Features.Auth.Commands.DenyDevice;
using ExoAuth.Application.Features.Auth.Commands.ResendDeviceApproval;
using ExoAuth.Application.Features.Auth.Commands.ResendPasswordReset;
using ExoAuth.Application.Features.Auth.Commands.RequestMagicLink;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Application.Features.Auth.Queries.GetCurrentUser;
using ExoAuth.Application.Features.Auth.Queries.GetDevices;
using ExoAuth.Application.Features.Auth.Commands.RevokeDevice;
using ExoAuth.Application.Features.Auth.Commands.RenameDevice;
using ExoAuth.Application.Features.Auth.Commands.ApproveDeviceFromSession;
using ExoAuth.Application.Features.Auth.Commands.PasskeyRegisterOptions;
using ExoAuth.Application.Features.Auth.Commands.PasskeyRegister;
using ExoAuth.Application.Features.Auth.Commands.PasskeyLoginOptions;
using ExoAuth.Application.Features.Auth.Commands.PasskeyLogin;
using ExoAuth.Application.Features.Auth.Commands.RenamePasskey;
using ExoAuth.Application.Features.Auth.Commands.DeletePasskey;
using ExoAuth.Application.Features.Auth.Queries.GetPasskeys;
using ExoAuth.Application.Features.SystemInvites.Models;
using ExoAuth.Application.Features.SystemInvites.Queries.ValidateInvite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExoAuth.Api.Controllers;

[Route("api/system/auth")]
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
    [RateLimit("sensitive")]
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
            request.CaptchaToken,
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
    [RateLimit("sensitive")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var command = new LoginCommand(
            request.Email,
            request.Password,
            request.CaptchaToken,
            request.DeviceId,
            request.DeviceFingerprint,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            request.RememberMe
        );

        var result = await Mediator.Send(command, ct);

        // Only set cookies when login is complete (not during MFA or device approval flow)
        if (!result.MfaRequired && !result.MfaSetupRequired && !result.DeviceApprovalRequired)
        {
            SetAuthCookies(result.AccessToken!, result.RefreshToken!);
        }

        return ApiOk(result);
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [RateLimit("mfa")]
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
    [RateLimit("mfa")]
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
    [RateLimit("mfa")]
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
    [RateLimit("sensitive")]
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

        // Only set cookies when registration is complete (not during MFA setup flow)
        if (!result.MfaSetupRequired && result.AccessToken != null && result.RefreshToken != null)
        {
            SetAuthCookies(result.AccessToken, result.RefreshToken);
        }

        return ApiOk(result);
    }

    /// <summary>
    /// Request a password reset email.
    /// </summary>
    [HttpPost("forgot-password")]
    [RateLimit("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        var command = new ForgotPasswordCommand(
            request.Email,
            request.CaptchaToken,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        var result = await Mediator.Send(command, ct);

        return ApiOk(result);
    }

    /// <summary>
    /// Resend a password reset email.
    /// Requires an existing password reset request.
    /// </summary>
    [HttpPost("forgot-password/resend")]
    [RateLimit("forgot-password")]
    [ProducesResponseType(typeof(ResendPasswordResetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendPasswordReset(ResendPasswordResetRequest request, CancellationToken ct)
    {
        var command = new ResendPasswordResetCommand(
            request.Email,
            request.CaptchaToken,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        var result = await Mediator.Send(command, ct);

        return ApiOk(result);
    }

    /// <summary>
    /// Reset password using token or code.
    /// </summary>
    [HttpPost("reset-password")]
    [RateLimit("sensitive")]
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

    /// <summary>
    /// Request a magic link email for passwordless login.
    /// </summary>
    [HttpPost("magic-link/request")]
    [RateLimit("forgot-password")]
    [ProducesResponseType(typeof(RequestMagicLinkResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestMagicLink(RequestMagicLinkRequest request, CancellationToken ct)
    {
        var command = new RequestMagicLinkCommand(
            request.Email,
            request.CaptchaToken,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        var result = await Mediator.Send(command, ct);

        return ApiOk(result);
    }

    #region Devices

    /// <summary>
    /// Get all devices for the current user (pending, trusted, and revoked).
    /// </summary>
    [HttpGet("devices")]
    [Authorize]
    [RateLimit]
    [ProducesResponseType(typeof(List<DeviceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDevices(CancellationToken ct)
    {
        var query = new GetDevicesQuery();
        var result = await Mediator.Send(query, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Revoke a device.
    /// Users cannot revoke the device they are currently using.
    /// </summary>
    [HttpDelete("devices/{deviceId:guid}")]
    [Authorize]
    [RateLimit]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeDevice(Guid deviceId, CancellationToken ct)
    {
        var command = new RevokeDeviceCommand(deviceId);
        var result = await Mediator.Send(command, ct);
        return result.Success ? NoContent() : BadRequest();
    }

    /// <summary>
    /// Rename a device.
    /// </summary>
    [HttpPut("devices/{deviceId:guid}/name")]
    [Authorize]
    [RateLimit]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RenameDevice(Guid deviceId, RenameDeviceRequest request, CancellationToken ct)
    {
        var command = new RenameDeviceCommand(deviceId, request.Name);
        var result = await Mediator.Send(command, ct);
        return result is not null ? ApiOk(result) : NotFound();
    }

    /// <summary>
    /// Approve a pending device from an existing trusted session.
    /// </summary>
    [HttpPost("devices/{deviceId:guid}/approve")]
    [Authorize]
    [RateLimit]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveDeviceFromSession(Guid deviceId, CancellationToken ct)
    {
        var command = new ApproveDeviceFromSessionCommand(deviceId);
        var result = await Mediator.Send(command, ct);
        return result.Success ? Ok() : BadRequest(result.Error);
    }

    #endregion

    #region Device Approval

    /// <summary>
    /// Approve a device using the approval token and XXXX-XXXX code.
    /// Called when user enters the code from the approval email.
    /// </summary>
    [HttpPost("approve-device")]
    [RateLimit("sensitive")]
    [ProducesResponseType(typeof(ApproveDeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ApproveDevice(ApproveDeviceRequest request, CancellationToken ct)
    {
        var command = new ApproveDeviceCommand(
            request.ApprovalToken,
            request.Code,
            request.CaptchaToken,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Approve a device using the link token from email.
    /// Called when user clicks the approval link in the email.
    /// Returns a redirect URL to the login page.
    /// </summary>
    [HttpGet("approve-device-link/{token}")]
    [RateLimit("mfa")]
    [ProducesResponseType(typeof(ApproveDeviceLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveDeviceLink([FromRoute] string token, CancellationToken ct)
    {
        var command = new ApproveDeviceLinkCommand(token);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Deny a device approval request.
    /// Called when user clicks the deny link in the email.
    /// This will revoke the device session and send a security alert.
    /// </summary>
    [HttpPost("deny-device")]
    [RateLimit("sensitive")]
    [ProducesResponseType(typeof(DenyDeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DenyDevice(DenyDeviceRequest request, CancellationToken ct)
    {
        var command = new DenyDeviceCommand(request.ApprovalToken);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Resend a device approval email.
    /// Requires the original approval token from the first email.
    /// </summary>
    [HttpPost("device-approval/resend")]
    [RateLimit("mfa")]
    [ProducesResponseType(typeof(ResendDeviceApprovalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendDeviceApproval(ResendDeviceApprovalRequest request, CancellationToken ct)
    {
        var command = new ResendDeviceApprovalCommand(
            request.ApprovalToken,
            request.CaptchaToken,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );
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
    [RateLimit("forgot-password")]
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
    [RateLimit("sensitive")]
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
    [RateLimit("sensitive")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MfaVerify(MfaVerifyRequest request, CancellationToken ct)
    {
        var command = new MfaVerifyCommand(
            request.MfaToken,
            request.Code,
            request.CaptchaToken,
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
    [RateLimit("forgot-password")]
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
    [RateLimit("forgot-password")]
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

    #region Passkeys

    /// <summary>
    /// Get WebAuthn registration options for creating a new passkey.
    /// Requires authenticated user.
    /// </summary>
    [HttpPost("passkeys/register/options")]
    [Authorize]
    [RateLimit("mfa")]
    [ProducesResponseType(typeof(PasskeyRegisterOptionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PasskeyRegisterOptions(CancellationToken ct)
    {
        var command = new PasskeyRegisterOptionsCommand();
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Complete passkey registration with the attestation response.
    /// </summary>
    [HttpPost("passkeys/register")]
    [Authorize]
    [RateLimit("sensitive")]
    [ProducesResponseType(typeof(PasskeyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PasskeyRegister(PasskeyRegisterRequest request, CancellationToken ct)
    {
        var command = new PasskeyRegisterCommand(
            request.ChallengeId,
            request.AttestationResponse,
            request.Name
        );
        var result = await Mediator.Send(command, ct);
        return ApiCreated(result);
    }

    /// <summary>
    /// Get WebAuthn assertion options for passkey login.
    /// </summary>
    [HttpPost("passkeys/login/options")]
    [RateLimit("mfa")]
    [ProducesResponseType(typeof(PasskeyLoginOptionsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> PasskeyLoginOptions(PasskeyLoginOptionsRequest? request, CancellationToken ct)
    {
        var command = new PasskeyLoginOptionsCommand(request?.Email);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Login with passkey using the assertion response.
    /// </summary>
    [HttpPost("passkeys/login")]
    [RateLimit("sensitive")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PasskeyLogin(PasskeyLoginRequest request, CancellationToken ct)
    {
        var command = new PasskeyLoginCommand(
            request.ChallengeId,
            request.AssertionResponse,
            request.DeviceId,
            request.DeviceFingerprint,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            request.RememberMe
        );
        var result = await Mediator.Send(command, ct);

        SetAuthCookies(result.AccessToken!, result.RefreshToken!);

        return ApiOk(result);
    }

    /// <summary>
    /// Get all passkeys for the current user.
    /// </summary>
    [HttpGet("passkeys")]
    [Authorize]
    [RateLimit]
    [ProducesResponseType(typeof(GetPasskeysResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPasskeys(CancellationToken ct)
    {
        var query = new GetPasskeysQuery();
        var result = await Mediator.Send(query, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Rename a passkey.
    /// </summary>
    [HttpPatch("passkeys/{passkeyId:guid}")]
    [Authorize]
    [RateLimit]
    [ProducesResponseType(typeof(PasskeyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RenamePasskey(Guid passkeyId, RenamePasskeyRequest request, CancellationToken ct)
    {
        var command = new RenamePasskeyCommand(passkeyId, request.Name);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Delete a passkey.
    /// </summary>
    [HttpDelete("passkeys/{passkeyId:guid}")]
    [Authorize]
    [RateLimit]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePasskey(Guid passkeyId, CancellationToken ct)
    {
        var command = new DeletePasskeyCommand(passkeyId);
        await Mediator.Send(command, ct);
        return NoContent();
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
            Path = "/api/system/auth",
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
            Path = "/api/system/auth",
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
    string? CaptchaToken = null,
    string? OrganizationName = null,
    string? DeviceId = null,
    string? DeviceFingerprint = null,
    string Language = "en-US"
);

public sealed record LoginRequest(
    string Email,
    string Password,
    string? CaptchaToken = null,
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
    string Email,
    string? CaptchaToken = null
);

public sealed record ResendPasswordResetRequest(
    string Email,
    string? CaptchaToken = null
);

public sealed record ResetPasswordRequest(
    string? Token,
    string? Email,
    string? Code,
    string NewPassword
);

public sealed record RequestMagicLinkRequest(
    string Email,
    string? CaptchaToken = null
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
    string? CaptchaToken = null,
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

public sealed record ApproveDeviceRequest(
    string ApprovalToken,
    string Code,
    string? CaptchaToken = null
);

public sealed record DenyDeviceRequest(
    string ApprovalToken
);

public sealed record ResendDeviceApprovalRequest(
    string ApprovalToken,
    string? CaptchaToken = null
);

public sealed record RenameDeviceRequest(
    string? Name
);

// Passkey Request DTOs
public sealed record PasskeyRegisterRequest(
    string ChallengeId,
    Fido2NetLib.AuthenticatorAttestationRawResponse AttestationResponse,
    string? Name = null
);

public sealed record PasskeyLoginOptionsRequest(
    string? Email = null
);

public sealed record PasskeyLoginRequest(
    string ChallengeId,
    Fido2NetLib.AuthenticatorAssertionRawResponse AssertionResponse,
    string? DeviceId = null,
    string? DeviceFingerprint = null,
    bool RememberMe = false
);

public sealed record RenamePasskeyRequest(
    string Name
);
