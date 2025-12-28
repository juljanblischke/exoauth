using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Enums;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.Login;

public sealed class LoginHandler : ICommandHandler<LoginCommand, AuthResponse>
{
    private readonly IAppDbContext _context;
    private readonly ISystemUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IBruteForceProtectionService _bruteForceService;
    private readonly IPermissionCacheService _permissionCache;
    private readonly IForceReauthService _forceReauthService;
    private readonly IDeviceSessionService _deviceSessionService;
    private readonly IAuditService _auditService;
    private readonly IMfaService _mfaService;

    public LoginHandler(
        IAppDbContext context,
        ISystemUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IBruteForceProtectionService bruteForceService,
        IPermissionCacheService permissionCache,
        IForceReauthService forceReauthService,
        IDeviceSessionService deviceSessionService,
        IAuditService auditService,
        IMfaService mfaService)
    {
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _bruteForceService = bruteForceService;
        _permissionCache = permissionCache;
        _forceReauthService = forceReauthService;
        _deviceSessionService = deviceSessionService;
        _auditService = auditService;
        _mfaService = mfaService;
    }

    public async ValueTask<AuthResponse> Handle(LoginCommand command, CancellationToken ct)
    {
        var email = command.Email.ToLowerInvariant();

        // Check if blocked due to too many attempts
        if (await _bruteForceService.IsBlockedAsync(email, ct))
        {
            await _auditService.LogWithContextAsync(
                AuditActions.LoginBlocked,
                null, // userId
                null, // targetUserId
                null, // entityType
                null, // entityId
                new { Email = email, Reason = "Too many failed attempts" },
                ct
            );
            throw new TooManyAttemptsException();
        }

        // Find user
        var user = await _userRepository.GetByEmailAsync(email, ct);

        if (user is null)
        {
            await RecordFailedAttempt(email, "User not found", ct);
            throw new InvalidCredentialsException();
        }

        // Verify password
        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            await RecordFailedAttempt(email, "Invalid password", ct);
            throw new InvalidCredentialsException();
        }

        // Check if active
        if (!user.IsActive)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.UserLoginFailed,
                user.Id,
                null, // targetUserId
                "SystemUser",
                user.Id,
                new { Reason = "User inactive" },
                ct
            );
            throw new UserInactiveException();
        }

        // Reset brute force counter on successful password verification
        await _bruteForceService.ResetAsync(email, ct);

        // Get permissions (with caching)
        var permissions = await _permissionCache.GetOrSetPermissionsAsync(
            user.Id,
            () => _userRepository.GetUserPermissionNamesAsync(user.Id, ct),
            ct
        );

        // Check if MFA is enabled
        if (user.MfaEnabled)
        {
            // Generate MFA token for the second step
            var mfaToken = _mfaService.GenerateMfaToken(user.Id, null);

            await _auditService.LogWithContextAsync(
                AuditActions.UserLoginFailed,
                user.Id,
                null,
                "SystemUser",
                user.Id,
                new { Reason = "MFA required", Step = "awaiting_mfa" },
                ct
            );

            return AuthResponse.RequiresMfa(mfaToken);
        }

        // Check if user has system permissions but MFA is not enabled
        // Users with system permissions MUST have MFA enabled
        var hasSystemPermissions = permissions.Any(p => p.StartsWith("system:"));
        if (hasSystemPermissions && !user.MfaEnabled)
        {
            // Generate setup token for MFA setup
            var setupToken = _mfaService.GenerateMfaToken(user.Id, null);

            await _auditService.LogWithContextAsync(
                AuditActions.UserLoginFailed,
                user.Id,
                null,
                "SystemUser",
                user.Id,
                new { Reason = "MFA setup required", Step = "awaiting_mfa_setup" },
                ct
            );

            return AuthResponse.RequiresMfaSetup(setupToken);
        }

        // Clear force re-auth flag if set
        await _forceReauthService.ClearFlagAsync(user.Id, ct);

        // Create or update device session
        var deviceId = command.DeviceId ?? _deviceSessionService.GenerateDeviceId();
        var (deviceSession, isNewDevice, isNewLocation) = await _deviceSessionService.CreateOrUpdateSessionAsync(
            user.Id,
            deviceId,
            command.UserAgent,
            command.IpAddress,
            command.DeviceFingerprint,
            ct
        );

        // Generate tokens with session ID
        var accessToken = _tokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            UserType.System,
            permissions,
            deviceSession.Id
        );

        var refreshTokenString = _tokenService.GenerateRefreshToken();
        var refreshToken = global::ExoAuth.Domain.Entities.RefreshToken.Create(
            userId: user.Id,
            userType: UserType.System,
            token: refreshTokenString,
            expirationDays: command.RememberMe ? 30 : (int)_tokenService.RefreshTokenExpiration.TotalDays
        );

        // Link refresh token to device session
        refreshToken.LinkToSession(deviceSession.Id);

        await _context.RefreshTokens.AddAsync(refreshToken, ct);
        await _context.SaveChangesAsync(ct);

        // Record login
        user.RecordLogin();
        await _userRepository.UpdateAsync(user, ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.UserLogin,
            user.Id,
            null, // targetUserId
            "SystemUser",
            user.Id,
            new
            {
                SessionId = deviceSession.Id,
                DeviceId = deviceId,
                IsNewDevice = isNewDevice,
                IsNewLocation = isNewLocation,
                RememberMe = command.RememberMe
            },
            ct
        );

        // Log new device/location events
        if (isNewDevice)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.LoginNewDevice,
                user.Id,
                null,
                "DeviceSession",
                deviceSession.Id,
                new { DeviceId = deviceId, Browser = deviceSession.Browser, Os = deviceSession.OperatingSystem },
                ct
            );
        }

        if (isNewLocation)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.LoginNewLocation,
                user.Id,
                null,
                "DeviceSession",
                deviceSession.Id,
                new { Country = deviceSession.Country, City = deviceSession.City },
                ct
            );
        }

        return new AuthResponse(
            User: new UserDto(
                Id: user.Id,
                Email: user.Email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                FullName: user.FullName,
                IsActive: user.IsActive,
                EmailVerified: user.EmailVerified,
                LastLoginAt: user.LastLoginAt,
                CreatedAt: user.CreatedAt,
                Permissions: permissions
            ),
            AccessToken: accessToken,
            RefreshToken: refreshTokenString,
            SessionId: deviceSession.Id,
            DeviceId: deviceId,
            IsNewDevice: isNewDevice,
            IsNewLocation: isNewLocation
        );
    }

    private async Task RecordFailedAttempt(string email, string reason, CancellationToken ct)
    {
        var (attempts, isBlocked) = await _bruteForceService.RecordFailedAttemptAsync(email, ct);

        await _auditService.LogWithContextAsync(
            AuditActions.UserLoginFailed,
            null, // userId
            null, // targetUserId
            null, // entityType
            null, // entityId
            new { Email = email, Reason = reason, Attempts = attempts, IsBlocked = isBlocked },
            ct
        );

        if (isBlocked)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.LoginBlocked,
                null, // userId
                null, // targetUserId
                null, // entityType
                null, // entityId
                new { Email = email, Attempts = attempts },
                ct
            );
        }
    }
}
