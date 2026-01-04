using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExoAuth.Application.Features.Auth.Commands.MfaVerify;

public sealed class MfaVerifyHandler : ICommandHandler<MfaVerifyCommand, AuthResponse>
{
    private readonly IAppDbContext _context;
    private readonly ISystemUserRepository _userRepository;
    private readonly IMfaService _mfaService;
    private readonly IEncryptionService _encryptionService;
    private readonly IBackupCodeService _backupCodeService;
    private readonly ITokenService _tokenService;
    private readonly IDeviceSessionService _deviceSessionService;
    private readonly IPermissionCacheService _permissionCache;
    private readonly IForceReauthService _forceReauthService;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IConfiguration _configuration;
    private readonly IRiskScoringService _riskScoringService;
    private readonly ILoginPatternService _loginPatternService;
    private readonly IDeviceApprovalService _deviceApprovalService;
    private readonly IGeoIpService _geoIpService;
    private readonly IDeviceDetectionService _deviceDetectionService;
    private readonly ITrustedDeviceService _trustedDeviceService;

    public MfaVerifyHandler(
        IAppDbContext context,
        ISystemUserRepository userRepository,
        IMfaService mfaService,
        IEncryptionService encryptionService,
        IBackupCodeService backupCodeService,
        ITokenService tokenService,
        IDeviceSessionService deviceSessionService,
        IPermissionCacheService permissionCache,
        IForceReauthService forceReauthService,
        IAuditService auditService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IConfiguration configuration,
        IRiskScoringService riskScoringService,
        ILoginPatternService loginPatternService,
        IDeviceApprovalService deviceApprovalService,
        IGeoIpService geoIpService,
        IDeviceDetectionService deviceDetectionService,
        ITrustedDeviceService trustedDeviceService)
    {
        _context = context;
        _userRepository = userRepository;
        _mfaService = mfaService;
        _encryptionService = encryptionService;
        _backupCodeService = backupCodeService;
        _tokenService = tokenService;
        _deviceSessionService = deviceSessionService;
        _permissionCache = permissionCache;
        _forceReauthService = forceReauthService;
        _auditService = auditService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _configuration = configuration;
        _riskScoringService = riskScoringService;
        _loginPatternService = loginPatternService;
        _deviceApprovalService = deviceApprovalService;
        _geoIpService = geoIpService;
        _deviceDetectionService = deviceDetectionService;
        _trustedDeviceService = trustedDeviceService;
    }

    public async ValueTask<AuthResponse> Handle(MfaVerifyCommand command, CancellationToken ct)
    {
        // Validate MFA token
        var tokenData = _mfaService.ValidateMfaToken(command.MfaToken)
            ?? throw new MfaTokenInvalidException();

        var userId = tokenData.userId;

        var user = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException();

        // Check user state (could have changed between password step and MFA step)
        if (!user.IsActive)
        {
            throw new UserInactiveException();
        }

        if (user.IsLocked)
        {
            throw new AccountLockedException(user.LockedUntil);
        }

        if (!user.MfaEnabled || string.IsNullOrEmpty(user.MfaSecret))
        {
            throw new MfaNotEnabledException();
        }

        // Normalize code (remove spaces, hyphens)
        var code = command.Code.Replace(" ", "").Replace("-", "").Trim();

        bool isBackupCode = false;
        bool codeValid = false;

        // Try TOTP code first (6 digits)
        if (code.Length == 6 && code.All(char.IsDigit))
        {
            var secret = _encryptionService.Decrypt(user.MfaSecret);
            codeValid = _mfaService.ValidateCode(secret, code);
        }
        else
        {
            // Try backup code (8 alphanumeric characters)
            var normalizedCode = _backupCodeService.NormalizeCode(code);
            if (normalizedCode.Length == 8)
            {
                var backupCode = await _context.MfaBackupCodes
                    .Where(c => c.UserId == userId && !c.IsUsed)
                    .ToListAsync(ct);

                var matchingCode = backupCode.FirstOrDefault(c =>
                    _backupCodeService.VerifyCode(normalizedCode, c.CodeHash));

                if (matchingCode != null)
                {
                    matchingCode.MarkAsUsed();
                    codeValid = true;
                    isBackupCode = true;

                    // Check remaining backup codes
                    var remainingCodes = await _context.MfaBackupCodes
                        .CountAsync(c => c.UserId == userId && !c.IsUsed, ct);

                    await _auditService.LogAsync(
                        AuditActions.MfaBackupCodeUsed,
                        userId,
                        null,
                        "MfaBackupCode",
                        matchingCode.Id,
                        new { RemainingCodes = remainingCodes },
                        ct
                    );

                    // Send notification email
                    await _emailService.SendAsync(
                        user.Email,
                        _emailTemplateService.GetSubject("mfa-backup-code-used", user.PreferredLanguage),
                        "mfa-backup-code-used",
                        new Dictionary<string, string>
                        {
                            ["firstName"] = user.FirstName,
                            ["remainingCodes"] = remainingCodes.ToString(),
                            ["year"] = DateTime.UtcNow.Year.ToString()
                        },
                        user.PreferredLanguage,
                        ct
                    );
                }
            }
        }

        if (!codeValid)
        {
            throw new MfaCodeInvalidException();
        }

        // Get permissions
        var permissions = await _permissionCache.GetOrSetPermissionsAsync(
            userId,
            () => _userRepository.GetUserPermissionNamesAsync(userId, ct),
            ct
        );

        // Get geo location and device info first (needed for trust check)
        var geoLocation = _geoIpService.GetLocation(command.IpAddress);
        var deviceInfo = _deviceDetectionService.Parse(command.UserAgent);
        var deviceId = command.DeviceId ?? _deviceSessionService.GenerateDeviceId();

        // Check if this device is trusted (Task 015: Trust check before risk scoring)
        var trustedDevice = await _trustedDeviceService.FindAsync(
            userId,
            deviceId,
            command.DeviceFingerprint,
            ct
        );

        // Create or update device session
        var (deviceSession, isNewDevice, isNewLocation) = await _deviceSessionService.CreateOrUpdateSessionAsync(
            userId,
            deviceId,
            command.UserAgent,
            command.IpAddress,
            command.DeviceFingerprint,
            ct
        );

        // If device is trusted, link the session to the trusted device
        if (trustedDevice is not null)
        {
            await _deviceSessionService.LinkToTrustedDeviceAsync(deviceSession.Id, trustedDevice.Id, ct);
            await _trustedDeviceService.RecordUsageAsync(trustedDevice.Id, command.IpAddress, geoLocation.CountryCode, geoLocation.City, ct);
        }

        // NEW DEVICE → Always require approval (Task 015)
        if (trustedDevice is null)
        {
            // Calculate risk score for email/audit purposes
            var riskScore = await _riskScoringService.CalculateAsync(
                userId,
                deviceInfo,
                geoLocation,
                false, // Not trusted
                ct
            );

            // Create device approval request
            var approvalResult = await _deviceApprovalService.CreateApprovalRequestAsync(
                userId,
                deviceSession.Id,
                riskScore.Score,
                riskScore.Factors,
                ct
            );

            // Send device approval email
            await _emailService.SendDeviceApprovalRequiredAsync(
                email: user.Email,
                firstName: user.FirstName,
                approvalToken: approvalResult.Token,
                approvalCode: approvalResult.Code,
                deviceName: deviceSession.DisplayName,
                browser: deviceSession.Browser,
                operatingSystem: deviceSession.OperatingSystem,
                location: deviceSession.LocationDisplay,
                ipAddress: deviceSession.IpAddress,
                riskScore: riskScore.Score,
                language: user.PreferredLanguage,
                cancellationToken: ct
            );

            // Audit log
            await _auditService.LogWithContextAsync(
                AuditActions.DeviceApprovalRequired,
                userId,
                null,
                "DeviceSession",
                deviceSession.Id,
                new
                {
                    riskScore.Score,
                    riskScore.Level,
                    riskScore.Factors,
                    DeviceId = deviceId,
                    IsNewDevice = isNewDevice,
                    IsNewLocation = isNewLocation,
                    IsBackupCode = isBackupCode,
                    Reason = "New device requires approval"
                },
                ct
            );

            return AuthResponse.RequiresDeviceApproval(
                approvalToken: approvalResult.Token,
                sessionId: deviceSession.Id,
                deviceId: deviceId,
                riskScore: riskScore.Score,
                riskLevel: riskScore.Level.ToString(),
                riskFactors: riskScore.Factors.ToList()
            );
        }

        // TRUSTED DEVICE → Check for spoofing (Task 015)
        var spoofingCheck = await _riskScoringService.CheckForSpoofingAsync(
            userId,
            trustedDevice,
            geoLocation,
            deviceInfo,
            ct
        );

        // If suspicious activity detected on trusted device, require re-verification
        if (spoofingCheck.IsSuspicious)
        {
            // Create device approval request
            var approvalResult = await _deviceApprovalService.CreateApprovalRequestAsync(
                userId,
                deviceSession.Id,
                spoofingCheck.RiskScore,
                spoofingCheck.SuspiciousFactors,
                ct
            );

            // Send device approval email
            await _emailService.SendDeviceApprovalRequiredAsync(
                email: user.Email,
                firstName: user.FirstName,
                approvalToken: approvalResult.Token,
                approvalCode: approvalResult.Code,
                deviceName: deviceSession.DisplayName,
                browser: deviceSession.Browser,
                operatingSystem: deviceSession.OperatingSystem,
                location: deviceSession.LocationDisplay,
                ipAddress: deviceSession.IpAddress,
                riskScore: spoofingCheck.RiskScore,
                language: user.PreferredLanguage,
                cancellationToken: ct
            );

            // Audit log
            await _auditService.LogWithContextAsync(
                AuditActions.DeviceApprovalRequired,
                userId,
                null,
                "DeviceSession",
                deviceSession.Id,
                new
                {
                    Score = spoofingCheck.RiskScore,
                    Level = "Suspicious",
                    Factors = spoofingCheck.SuspiciousFactors,
                    DeviceId = deviceId,
                    IsNewDevice = isNewDevice,
                    IsNewLocation = isNewLocation,
                    IsBackupCode = isBackupCode,
                    Reason = "Suspicious activity on trusted device"
                },
                ct
            );

            return AuthResponse.RequiresDeviceApproval(
                approvalToken: approvalResult.Token,
                sessionId: deviceSession.Id,
                deviceId: deviceId,
                riskScore: spoofingCheck.RiskScore,
                riskLevel: "Suspicious",
                riskFactors: spoofingCheck.SuspiciousFactors.ToList()
            );
        }

        // Clear force re-auth flag for this session (session-based, not user-based)
        await _forceReauthService.ClearFlagAsync(deviceSession.Id, ct);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(
            userId,
            user.Email,
            UserType.System,
            permissions,
            deviceSession.Id
        );

        var refreshTokenString = _tokenService.GenerateRefreshToken();
        var refreshToken = global::ExoAuth.Domain.Entities.RefreshToken.Create(
            userId: userId,
            userType: UserType.System,
            token: refreshTokenString,
            expirationDays: command.RememberMe ? 30 : (int)_tokenService.RefreshTokenExpiration.TotalDays
        );

        refreshToken.LinkToSession(deviceSession.Id);

        await _context.RefreshTokens.AddAsync(refreshToken, ct);
        await _context.SaveChangesAsync(ct);

        // Record login
        user.RecordLogin();
        await _userRepository.UpdateAsync(user, ct);

        // Record login pattern for future risk scoring
        await _loginPatternService.RecordLoginAsync(
            userId,
            geoLocation,
            deviceInfo.DeviceType,
            command.IpAddress,
            ct
        );

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.MfaVerified,
            userId,
            null,
            "SystemUser",
            userId,
            new
            {
                SessionId = deviceSession.Id,
                DeviceId = deviceId,
                IsNewDevice = isNewDevice,
                IsNewLocation = isNewLocation,
                IsBackupCode = isBackupCode,
                TrustedDeviceId = trustedDevice?.Id,
                IsTrustedDevice = true
            },
            ct
        );

        // Log new device/location events and send notification emails
        if (isNewDevice)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.LoginNewDevice,
                userId,
                null,
                "DeviceSession",
                deviceSession.Id,
                new { DeviceId = deviceId, Browser = deviceSession.Browser, Os = deviceSession.OperatingSystem },
                ct
            );

            await SendNewDeviceEmailAsync(user, deviceSession, ct);
        }

        if (isNewLocation)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.LoginNewLocation,
                userId,
                null,
                "DeviceSession",
                deviceSession.Id,
                new { Country = deviceSession.Country, City = deviceSession.City },
                ct
            );

            // Send new location notification email (only if not already sent new device email)
            if (!isNewDevice)
            {
                await SendNewLocationEmailAsync(user, deviceSession, ct);
            }
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
            SessionId: deviceSession.Id,
            DeviceId: deviceId,
            IsNewDevice: isNewDevice,
            IsNewLocation: isNewLocation
        );
    }

    private async Task SendNewDeviceEmailAsync(
        Domain.Entities.SystemUser user,
        Domain.Entities.DeviceSession deviceSession,
        CancellationToken ct)
    {
        var baseUrl = _configuration["SystemInvite:BaseUrl"] ?? "http://localhost:5173";
        var deviceName = !string.IsNullOrEmpty(deviceSession.OperatingSystem)
            ? $"{deviceSession.OperatingSystem}"
            : "Unknown Device";
        var location = !string.IsNullOrEmpty(deviceSession.City) && !string.IsNullOrEmpty(deviceSession.Country)
            ? $"{deviceSession.City}, {deviceSession.Country}"
            : deviceSession.Country ?? "Unknown Location";

        var variables = new Dictionary<string, string>
        {
            ["firstName"] = user.FirstName,
            ["deviceName"] = deviceName,
            ["browser"] = deviceSession.Browser ?? "Unknown Browser",
            ["location"] = location,
            ["ipAddress"] = deviceSession.IpAddress ?? "Unknown",
            ["loginTime"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"),
            ["sessionsUrl"] = $"{baseUrl}/settings/sessions",
            ["changePasswordUrl"] = $"{baseUrl}/settings/security",
            ["year"] = DateTime.UtcNow.Year.ToString()
        };

        await _emailService.SendAsync(
            to: user.Email,
            subject: _emailTemplateService.GetSubject("new-device-login", user.PreferredLanguage),
            templateName: "new-device-login",
            variables: variables,
            language: user.PreferredLanguage,
            cancellationToken: ct
        );
    }

    private async Task SendNewLocationEmailAsync(
        Domain.Entities.SystemUser user,
        Domain.Entities.DeviceSession deviceSession,
        CancellationToken ct)
    {
        var baseUrl = _configuration["SystemInvite:BaseUrl"] ?? "http://localhost:5173";
        var deviceName = !string.IsNullOrEmpty(deviceSession.OperatingSystem)
            ? $"{deviceSession.OperatingSystem}"
            : "Unknown Device";
        var newLocation = !string.IsNullOrEmpty(deviceSession.City) && !string.IsNullOrEmpty(deviceSession.Country)
            ? $"{deviceSession.City}, {deviceSession.Country}"
            : deviceSession.Country ?? "Unknown Location";

        var variables = new Dictionary<string, string>
        {
            ["firstName"] = user.FirstName,
            ["newLocation"] = newLocation,
            ["previousLocation"] = "Your usual location",
            ["deviceName"] = deviceName,
            ["ipAddress"] = deviceSession.IpAddress ?? "Unknown",
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
