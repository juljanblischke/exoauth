using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.Extensions.Configuration;

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
    private readonly IDeviceService _deviceService;
    private readonly IAuditService _auditService;
    private readonly IMfaService _mfaService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IConfiguration _configuration;
    private readonly IRiskScoringService _riskScoringService;
    private readonly ILoginPatternService _loginPatternService;
    private readonly IGeoIpService _geoIpService;
    private readonly IDeviceDetectionService _deviceDetectionService;

    public LoginHandler(
        IAppDbContext context,
        ISystemUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IBruteForceProtectionService bruteForceService,
        IPermissionCacheService permissionCache,
        IForceReauthService forceReauthService,
        IDeviceService deviceService,
        IAuditService auditService,
        IMfaService mfaService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IConfiguration configuration,
        IRiskScoringService riskScoringService,
        ILoginPatternService loginPatternService,
        IGeoIpService geoIpService,
        IDeviceDetectionService deviceDetectionService)
    {
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _bruteForceService = bruteForceService;
        _permissionCache = permissionCache;
        _forceReauthService = forceReauthService;
        _deviceService = deviceService;
        _auditService = auditService;
        _mfaService = mfaService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _configuration = configuration;
        _riskScoringService = riskScoringService;
        _loginPatternService = loginPatternService;
        _geoIpService = geoIpService;
        _deviceDetectionService = deviceDetectionService;
    }

    public async ValueTask<AuthResponse> Handle(LoginCommand command, CancellationToken ct)
    {
        var email = command.Email.ToLowerInvariant();

        // Check if blocked due to too many attempts (Redis-based progressive lockout)
        if (await _bruteForceService.IsBlockedAsync(email, ct))
        {
            var lockoutStatus = await _bruteForceService.GetLockoutStatusAsync(email, ct);

            await _auditService.LogWithContextAsync(
                AuditActions.LoginBlocked,
                null, // userId
                null, // targetUserId
                null, // entityType
                null, // entityId
                new { Email = email, Reason = "Account temporarily locked", LockedUntil = lockoutStatus?.LockedUntil },
                ct
            );

            throw new AccountLockedException(lockoutStatus?.LockedUntil);
        }

        // Find user
        var user = await _userRepository.GetByEmailAsync(email, ct);

        if (user is null)
        {
            await RecordFailedAttempt(email, null, "User not found", ct);
            throw new InvalidCredentialsException();
        }

        // Check if user is locked in the database (by admin or previous lockout)
        if (user.IsLocked)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.LoginBlocked,
                user.Id,
                null,
                "SystemUser",
                user.Id,
                new { Reason = "Account locked", LockedUntil = user.LockedUntil },
                ct
            );

            throw new AccountLockedException(user.LockedUntil);
        }

        // Verify password
        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            await RecordFailedAttempt(email, user, "Invalid password", ct);
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

        // Also reset the failed login attempts on the user entity
        if (user.FailedLoginAttempts > 0)
        {
            user.ResetFailedLoginAttempts();
            await _userRepository.UpdateAsync(user, ct);
        }

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
                AuditActions.MfaChallengeSent,
                user.Id,
                null,
                "SystemUser",
                user.Id,
                new { Step = "awaiting_mfa" },
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
                AuditActions.MfaSetupRequiredSent,
                user.Id,
                null,
                "SystemUser",
                user.Id,
                new { Step = "awaiting_mfa_setup" },
                ct
            );

            return AuthResponse.RequiresMfaSetup(setupToken);
        }

        // Get geo location and device info (needed for trust check and device creation)
        var geoLocation = _geoIpService.GetLocation(command.IpAddress);
        var deviceInfo = _deviceDetectionService.Parse(command.UserAgent);
        var deviceId = command.DeviceId ?? _deviceService.GenerateDeviceId();

        // Check if this device is trusted
        var device = await _deviceService.FindTrustedDeviceAsync(
            user.Id,
            deviceId,
            command.DeviceFingerprint,
            ct
        );

        // NEW DEVICE → Always require approval
        if (device is null)
        {
            // Calculate risk score for email/audit purposes
            var riskScore = await _riskScoringService.CalculateAsync(
                user.Id,
                deviceInfo,
                geoLocation,
                false, // Not trusted
                ct
            );

            // Create pending device with approval credentials
            var pendingResult = await _deviceService.CreatePendingDeviceAsync(
                user.Id,
                deviceId,
                riskScore.Score,
                riskScore.Factors,
                deviceInfo,
                geoLocation,
                command.DeviceFingerprint,
                ct
            );

            var pendingDevice = pendingResult.Device;

            // Send device approval email
            await _emailService.SendDeviceApprovalRequiredAsync(
                email: user.Email,
                firstName: user.FirstName,
                approvalToken: pendingResult.ApprovalToken,
                approvalCode: pendingResult.ApprovalCode,
                deviceName: pendingDevice.DisplayName,
                browser: pendingDevice.Browser,
                operatingSystem: pendingDevice.OperatingSystem,
                location: pendingDevice.LocationDisplay,
                ipAddress: pendingDevice.IpAddress,
                riskScore: riskScore.Score,
                language: user.PreferredLanguage,
                cancellationToken: ct
            );

            // Audit log
            await _auditService.LogWithContextAsync(
                AuditActions.DeviceApprovalRequired,
                user.Id,
                null,
                "Device",
                pendingDevice.Id,
                new
                {
                    riskScore.Score,
                    riskScore.Level,
                    riskScore.Factors,
                    DeviceId = deviceId,
                    Reason = "New device requires approval"
                },
                ct
            );

            return AuthResponse.RequiresDeviceApproval(
                approvalToken: pendingResult.ApprovalToken,
                sessionId: pendingDevice.Id,
                deviceId: deviceId,
                riskScore: riskScore.Score,
                riskLevel: riskScore.Level.ToString(),
                riskFactors: riskScore.Factors.ToList()
            );
        }

        // Check if location has changed
        var isNewLocation = !string.Equals(device.CountryCode, geoLocation.CountryCode, StringComparison.OrdinalIgnoreCase)
                         || !string.Equals(device.City, geoLocation.City, StringComparison.OrdinalIgnoreCase);

        // TRUSTED DEVICE → Check for spoofing
        var spoofingCheck = await _riskScoringService.CheckForSpoofingAsync(
            user.Id,
            device,
            geoLocation,
            deviceInfo,
            ct
        );

        // If suspicious activity detected on trusted device, require re-verification
        if (spoofingCheck.IsSuspicious)
        {
            // Create a new pending device for re-approval
            var pendingResult = await _deviceService.CreatePendingDeviceAsync(
                user.Id,
                deviceId,
                spoofingCheck.RiskScore,
                spoofingCheck.SuspiciousFactors,
                deviceInfo,
                geoLocation,
                command.DeviceFingerprint,
                ct
            );

            var pendingDevice = pendingResult.Device;

            // Send device approval email
            await _emailService.SendDeviceApprovalRequiredAsync(
                email: user.Email,
                firstName: user.FirstName,
                approvalToken: pendingResult.ApprovalToken,
                approvalCode: pendingResult.ApprovalCode,
                deviceName: pendingDevice.DisplayName,
                browser: pendingDevice.Browser,
                operatingSystem: pendingDevice.OperatingSystem,
                location: pendingDevice.LocationDisplay,
                ipAddress: pendingDevice.IpAddress,
                riskScore: spoofingCheck.RiskScore,
                language: user.PreferredLanguage,
                cancellationToken: ct
            );

            // Audit log
            await _auditService.LogWithContextAsync(
                AuditActions.DeviceApprovalRequired,
                user.Id,
                null,
                "Device",
                pendingDevice.Id,
                new
                {
                    Score = spoofingCheck.RiskScore,
                    Level = "Suspicious",
                    Factors = spoofingCheck.SuspiciousFactors,
                    DeviceId = deviceId,
                    IsNewLocation = isNewLocation,
                    Reason = "Suspicious activity on trusted device"
                },
                ct
            );

            return AuthResponse.RequiresDeviceApproval(
                approvalToken: pendingResult.ApprovalToken,
                sessionId: pendingDevice.Id,
                deviceId: deviceId,
                riskScore: spoofingCheck.RiskScore,
                riskLevel: "Suspicious",
                riskFactors: spoofingCheck.SuspiciousFactors.ToList()
            );
        }

        // Record device usage (updates last used timestamp and location)
        await _deviceService.RecordUsageAsync(device.Id, command.IpAddress, geoLocation.CountryCode, geoLocation.City, ct);

        // Clear force re-auth flag for this device session
        await _forceReauthService.ClearFlagAsync(device.Id, ct);

        // Generate tokens with device ID as session ID
        var accessToken = _tokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            UserType.System,
            permissions,
            device.Id
        );

        var refreshTokenString = _tokenService.GenerateRefreshToken();
        var refreshToken = global::ExoAuth.Domain.Entities.RefreshToken.Create(
            userId: user.Id,
            userType: UserType.System,
            token: refreshTokenString,
            expirationDays: command.RememberMe ? _tokenService.RememberMeExpirationDays : (int)_tokenService.RefreshTokenExpiration.TotalDays
        );

        // Link refresh token to device
        refreshToken.LinkToDevice(device.Id);

        await _context.RefreshTokens.AddAsync(refreshToken, ct);
        await _context.SaveChangesAsync(ct);

        // Record login
        user.RecordLogin();
        await _userRepository.UpdateAsync(user, ct);

        // Record login pattern for future risk scoring
        await _loginPatternService.RecordLoginAsync(
            user.Id,
            geoLocation,
            deviceInfo.DeviceType,
            command.IpAddress,
            ct
        );

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.UserLogin,
            user.Id,
            null, // targetUserId
            "SystemUser",
            user.Id,
            new
            {
                SessionId = device.Id,
                DeviceId = deviceId,
                IsNewLocation = isNewLocation,
                RememberMe = command.RememberMe,
                IsTrustedDevice = true
            },
            ct
        );

        // Send notification email for new location (only for trusted device logins)
        if (isNewLocation)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.LoginNewLocation,
                user.Id,
                null,
                "Device",
                device.Id,
                new { Country = geoLocation.CountryCode, City = geoLocation.City },
                ct
            );

            await SendNewLocationEmailAsync(user, device, geoLocation, ct);
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
            IsNewDevice: false, // We found a trusted device, so not new
            IsNewLocation: isNewLocation
        );
    }

    private async Task RecordFailedAttempt(string email, Domain.Entities.SystemUser? user, string reason, CancellationToken ct)
    {
        var lockoutResult = await _bruteForceService.RecordFailedAttemptAsync(email, ct);

        // Update user entity if found
        if (user is not null)
        {
            user.RecordFailedLogin();

            // If locked, also lock in the database
            if (lockoutResult.IsLocked && lockoutResult.LockedUntil.HasValue)
            {
                user.Lock(lockoutResult.LockedUntil);
            }

            await _userRepository.UpdateAsync(user, ct);
        }

        await _auditService.LogWithContextAsync(
            AuditActions.UserLoginFailed,
            user?.Id,
            null, // targetUserId
            user is not null ? "SystemUser" : null,
            user?.Id,
            new
            {
                Email = email,
                Reason = reason,
                Attempts = lockoutResult.Attempts,
                IsLocked = lockoutResult.IsLocked,
                LockedUntil = lockoutResult.LockedUntil,
                LockoutSeconds = lockoutResult.LockoutSeconds
            },
            ct
        );

        if (lockoutResult.IsLocked)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.AccountLocked,
                user?.Id,
                null,
                user is not null ? "SystemUser" : null,
                user?.Id,
                new
                {
                    Email = email,
                    Attempts = lockoutResult.Attempts,
                    LockedUntil = lockoutResult.LockedUntil,
                    LockoutSeconds = lockoutResult.LockoutSeconds
                },
                ct
            );

            // Send email notification if user exists and should notify (lockout >= NotifyAfterSeconds)
            if (user is not null && lockoutResult.ShouldNotify)
            {
                await SendAccountLockedEmailAsync(user, lockoutResult, ct);
            }

            // Throw locked exception after recording everything
            throw new AccountLockedException(lockoutResult.LockedUntil);
        }
    }

    private async Task SendAccountLockedEmailAsync(
        Domain.Entities.SystemUser user,
        LockoutResult lockoutResult,
        CancellationToken ct)
    {
        var lockoutMinutes = lockoutResult.LockoutSeconds / 60;
        var lockedUntilFormatted = lockoutResult.LockedUntil?.ToString("HH:mm 'UTC'") ?? "Unknown";

        var variables = new Dictionary<string, string>
        {
            ["firstName"] = user.FirstName,
            ["email"] = user.Email,
            ["attempts"] = lockoutResult.Attempts.ToString(),
            ["lockoutMinutes"] = lockoutMinutes.ToString(),
            ["lockedUntil"] = lockedUntilFormatted,
            ["year"] = DateTime.UtcNow.Year.ToString()
        };

        await _emailService.SendAsync(
            to: user.Email,
            subject: _emailTemplateService.GetSubject("account-locked", user.PreferredLanguage),
            templateName: "account-locked",
            variables: variables,
            language: user.PreferredLanguage,
            cancellationToken: ct
        );
    }

    private async Task SendNewLocationEmailAsync(
        Domain.Entities.SystemUser user,
        Domain.Entities.Device device,
        GeoLocation newLocation,
        CancellationToken ct)
    {
        var baseUrl = _configuration["SystemInvite:BaseUrl"] ?? "http://localhost:5173";
        var deviceName = device.DisplayName;
        var locationDisplay = !string.IsNullOrEmpty(newLocation.City) && !string.IsNullOrEmpty(newLocation.Country)
            ? $"{newLocation.City}, {newLocation.Country}"
            : newLocation.Country ?? "Unknown Location";

        var variables = new Dictionary<string, string>
        {
            ["firstName"] = user.FirstName,
            ["newLocation"] = locationDisplay,
            ["previousLocation"] = device.LocationDisplay ?? "Your usual location",
            ["deviceName"] = deviceName,
            ["ipAddress"] = device.IpAddress ?? "Unknown",
            ["loginTime"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"),
            ["sessionsUrl"] = $"{baseUrl}/settings/sessions",
            ["changePasswordUrl"] = $"{baseUrl}/settings/security",
            ["year"] = DateTime.UtcNow.Year.ToString()
        };

        await _emailService.SendAsync(
            to: user.Email,
            subject: _emailTemplateService.GetSubject("new-location-login", user.PreferredLanguage),
            templateName: "new-location-login",
            variables: variables,
            language: user.PreferredLanguage,
            cancellationToken: ct
        );
    }
}
