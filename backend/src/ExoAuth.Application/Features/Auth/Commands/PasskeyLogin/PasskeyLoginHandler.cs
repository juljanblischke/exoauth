using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.PasskeyLogin;

public sealed class PasskeyLoginHandler : ICommandHandler<PasskeyLoginCommand, AuthResponse>
{
    private readonly IAppDbContext _context;
    private readonly IPasskeyService _passkeyService;
    private readonly ITokenService _tokenService;
    private readonly IPermissionCacheService _permissionCache;
    private readonly ISystemUserRepository _userRepository;
    private readonly IForceReauthService _forceReauthService;
    private readonly IDeviceService _deviceService;
    private readonly IAuditService _auditService;
    private readonly ILoginPatternService _loginPatternService;
    private readonly IGeoIpService _geoIpService;
    private readonly IDeviceDetectionService _deviceDetectionService;

    public PasskeyLoginHandler(
        IAppDbContext context,
        IPasskeyService passkeyService,
        ITokenService tokenService,
        IPermissionCacheService permissionCache,
        ISystemUserRepository userRepository,
        IForceReauthService forceReauthService,
        IDeviceService deviceService,
        IAuditService auditService,
        ILoginPatternService loginPatternService,
        IGeoIpService geoIpService,
        IDeviceDetectionService deviceDetectionService)
    {
        _context = context;
        _passkeyService = passkeyService;
        _tokenService = tokenService;
        _permissionCache = permissionCache;
        _userRepository = userRepository;
        _forceReauthService = forceReauthService;
        _deviceService = deviceService;
        _auditService = auditService;
        _loginPatternService = loginPatternService;
        _geoIpService = geoIpService;
        _deviceDetectionService = deviceDetectionService;
    }

    public async ValueTask<AuthResponse> Handle(PasskeyLoginCommand command, CancellationToken ct)
    {
        // Find the passkey by credential ID from the assertion response
        var credentialId = command.AssertionResponse.Id;
        
        var passkey = await _context.Passkeys
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.CredentialId.SequenceEqual(credentialId), ct);

        if (passkey is null)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.PasskeyLoginFailed,
                null,
                null,
                "Passkey",
                null,
                new { Reason = "Passkey not found" },
                ct
            );
            throw new PasskeyInvalidCredentialException();
        }

        var user = passkey.User!;

        // Check if user is active
        if (!user.IsActive)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.PasskeyLoginFailed,
                user.Id,
                null,
                "Passkey",
                passkey.Id,
                new { Reason = "User inactive" },
                ct
            );
            throw new UserInactiveException();
        }

        // Check if user is locked
        if (user.IsLocked)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.PasskeyLoginFailed,
                user.Id,
                null,
                "Passkey",
                passkey.Id,
                new { Reason = "Account locked", LockedUntil = user.LockedUntil },
                ct
            );
            throw new AccountLockedException(user.LockedUntil);
        }

        // Verify the assertion
        var newCounter = await _passkeyService.VerifyLoginAsync(
            command.ChallengeId,
            command.AssertionResponse,
            passkey.CredentialId,
            passkey.PublicKey,
            passkey.Counter,
            ct);

        if (newCounter is null)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.PasskeyLoginFailed,
                user.Id,
                null,
                "Passkey",
                passkey.Id,
                new { Reason = "Assertion verification failed" },
                ct
            );
            throw new PasskeyInvalidCredentialException();
        }

        // Update passkey counter
        passkey.UpdateCounter(newCounter.Value);
        await _context.SaveChangesAsync(ct);

        // Get geo location and device info
        var geoLocation = _geoIpService.GetLocation(command.IpAddress);
        var deviceInfo = _deviceDetectionService.Parse(command.UserAgent);
        var deviceId = command.DeviceId ?? _deviceService.GenerateDeviceId();

        // Find or create trusted device (passkey login = trusted)
        var device = await _deviceService.FindTrustedDeviceAsync(
            user.Id,
            deviceId,
            command.DeviceFingerprint,
            ct);

        if (device is null)
        {
            // Create trusted device directly for passkey login
            device = await _deviceService.CreateTrustedDeviceAsync(
                user.Id,
                deviceId,
                deviceInfo,
                geoLocation,
                command.DeviceFingerprint,
                ct);
        }
        else
        {
            // Record device usage
            await _deviceService.RecordUsageAsync(device.Id, command.IpAddress, geoLocation.CountryCode, geoLocation.City, ct);
        }

        // Get permissions
        var permissions = await _permissionCache.GetOrSetPermissionsAsync(
            user.Id,
            () => _userRepository.GetUserPermissionNamesAsync(user.Id, ct),
            ct
        );

        // Clear force re-auth flag
        await _forceReauthService.ClearFlagAsync(device.Id, ct);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            UserType.System,
            permissions,
            device.Id
        );

        var refreshTokenString = _tokenService.GenerateRefreshToken();
        var refreshToken = Domain.Entities.RefreshToken.Create(
            userId: user.Id,
            userType: UserType.System,
            token: refreshTokenString,
            expirationDays: command.RememberMe ? _tokenService.RememberMeExpirationDays : (int)_tokenService.RefreshTokenExpiration.TotalDays
        );

        refreshToken.LinkToDevice(device.Id);

        await _context.RefreshTokens.AddAsync(refreshToken, ct);
        await _context.SaveChangesAsync(ct);

        // Record login
        user.RecordLogin();
        await _userRepository.UpdateAsync(user, ct);

        // Record login pattern
        await _loginPatternService.RecordLoginAsync(
            user.Id,
            geoLocation,
            deviceInfo.DeviceType,
            command.IpAddress,
            ct
        );

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.PasskeyLogin,
            user.Id,
            null,
            "Passkey",
            passkey.Id,
            new
            {
                SessionId = device.Id,
                DeviceId = deviceId,
                PasskeyName = passkey.Name,
                RememberMe = command.RememberMe
            },
            ct
        );

        return new AuthResponse(
            User: new UserDto(
                Id: user.Id,
                Email: user.Email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                FullName: user.FullName,
                IsActive: user.IsActive,
                EmailVerified: user.EmailVerified,
                MfaEnabled: user.MfaEnabled,
                PreferredLanguage: user.PreferredLanguage,
                LastLoginAt: user.LastLoginAt,
                CreatedAt: user.CreatedAt,
                Permissions: permissions
            ),
            AccessToken: accessToken,
            RefreshToken: refreshTokenString,
            SessionId: device.Id,
            DeviceId: deviceId,
            IsNewDevice: false,
            IsNewLocation: false
        );
    }
}
