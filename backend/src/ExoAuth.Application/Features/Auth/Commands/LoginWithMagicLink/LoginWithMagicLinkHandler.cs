using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.LoginWithMagicLink;

public sealed class LoginWithMagicLinkHandler : ICommandHandler<LoginWithMagicLinkCommand, AuthResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMagicLinkService _magicLinkService;
    private readonly ISystemUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPermissionCacheService _permissionCache;
    private readonly IForceReauthService _forceReauthService;
    private readonly IRevokedSessionService _revokedSessionService;
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
    private readonly ILogger<LoginWithMagicLinkHandler> _logger;

    public LoginWithMagicLinkHandler(
        IAppDbContext context,
        IMagicLinkService magicLinkService,
        ISystemUserRepository userRepository,
        ITokenService tokenService,
        IPermissionCacheService permissionCache,
        IForceReauthService forceReauthService,
        IRevokedSessionService revokedSessionService,
        IDeviceService deviceService,
        IAuditService auditService,
        IMfaService mfaService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IConfiguration configuration,
        IRiskScoringService riskScoringService,
        ILoginPatternService loginPatternService,
        IGeoIpService geoIpService,
        IDeviceDetectionService deviceDetectionService,
        ILogger<LoginWithMagicLinkHandler> logger)
    {
        _context = context;
        _magicLinkService = magicLinkService;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _permissionCache = permissionCache;
        _forceReauthService = forceReauthService;
        _revokedSessionService = revokedSessionService;
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
        _logger = logger;
    }

    public async ValueTask<AuthResponse> Handle(LoginWithMagicLinkCommand command, CancellationToken ct)
    {
        // Validate magic link token
        var magicLinkToken = await _magicLinkService.ValidateTokenAsync(command.Token, ct);

        if (magicLinkToken is null)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.MagicLinkLoginFailed,
                null,
                null,
                null,
                null,
                new { Reason = "Invalid or expired token" },
                ct
            );

            throw new MagicLinkTokenInvalidException();
        }

        // Get the user
        var user = await _userRepository.GetByIdAsync(magicLinkToken.UserId, ct);

        if (user is null || !user.IsActive || user.IsLocked)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.MagicLinkLoginFailed,
                user?.Id,
                null,
                "SystemUser",
                user?.Id,
                new { Reason = user is null ? "User not found" : user.IsLocked ? "Account locked" : "User inactive" },
                ct
            );

            throw new MagicLinkTokenInvalidException();
        }

        // Mark token as used
        await _magicLinkService.MarkAsUsedAsync(magicLinkToken, ct);

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
                new { Step = "awaiting_mfa", Method = "magic_link" },
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
                new { Step = "awaiting_mfa_setup", Method = "magic_link" },
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
                userId: user.Id,
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
                    Reason = "New device requires approval",
                    Method = "magic_link"
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
                userId: user.Id,
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
                    Reason = "Suspicious activity on trusted device",
                    Method = "magic_link"
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

        // Clear force re-auth flag and revoked session status for this device
        await _forceReauthService.ClearFlagAsync(device.Id, ct);
        await _revokedSessionService.ClearRevokedSessionAsync(device.Id, ct);

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
            AuditActions.MagicLinkLogin,
            user.Id,
            null,
            "SystemUser",
            user.Id,
            new
            {
                SessionId = device.Id,
                DeviceId = deviceId,
                IsNewLocation = isNewLocation,
                RememberMe = command.RememberMe,
                IsTrustedDevice = true,
                Method = "magic_link"
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
                new { Country = geoLocation.CountryCode, City = geoLocation.City, Method = "magic_link" },
                ct
            );

            await SendNewLocationEmailAsync(user, device, geoLocation, ct);
        }

        _logger.LogInformation("User {UserId} logged in successfully with magic link", user.Id);

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
            recipientUserId: user.Id,
            cancellationToken: ct
        );
    }
}
