using ExoAuth.Api.Filters;
using ExoAuth.Application.Features.Auth.Commands.AcceptInvite;
using ExoAuth.Application.Features.Auth.Commands.Login;
using ExoAuth.Application.Features.Auth.Commands.Logout;
using ExoAuth.Application.Features.Auth.Commands.RefreshToken;
using ExoAuth.Application.Features.Auth.Commands.Register;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Application.Features.Auth.Queries.GetCurrentUser;
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
            request.OrganizationName
        );

        var result = await Mediator.Send(command, ct);

        SetAuthCookies(result.AccessToken, result.RefreshToken);

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
        var command = new LoginCommand(request.Email, request.Password);

        var result = await Mediator.Send(command, ct);

        SetAuthCookies(result.AccessToken, result.RefreshToken);

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

        var command = new RefreshTokenCommand(refreshToken);

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
        var command = new AcceptInviteCommand(request.Token, request.Password);

        var result = await Mediator.Send(command, ct);

        SetAuthCookies(result.AccessToken, result.RefreshToken);

        return ApiOk(result);
    }

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
    string? OrganizationName = null
);

public sealed record LoginRequest(
    string Email,
    string Password
);

public sealed record RefreshTokenRequest(
    string? RefreshToken = null
);

public sealed record LogoutRequest(
    string? RefreshToken = null
);

public sealed record AcceptInviteRequest(
    string Token,
    string Password
);
