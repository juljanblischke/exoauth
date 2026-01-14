using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
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
    private readonly IDeviceService _deviceService;
    private readonly IPermissionCacheService _permissionCache;
    private readonly IForceReauthService _forceReauthService;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IConfiguration _configuration;
    private readonly IRiskScoringService _riskScoringService;
    private readonly ILoginPatternService _loginPatternService;
    private readonly IGeoIpService _geoIpService;
    private readonly IDeviceDetectionService _deviceDetectionService;
    private readonly ICaptchaService _captchaService;

    public MfaVerifyHandler(
        IAppDbContext context,
        ISystemUserRepository userRepository,
        IMfaService mfaService,
        IEncryptionService encryptionService,
        IBackupCodeService backupCodeService,
        ITokenService tokenService,
        IDeviceService deviceService,
        IPermissionCacheService permissionCache,
        IForceReauthService forceReauthService,
        IRevokedSessionService revokedSessionService,
        IAuditService auditService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IConfiguration configuration,
        IRiskScoringService riskScoringService,
        ILoginPatternService loginPatternService,
        IGeoIpService geoIpService,
        IDeviceDetectionService deviceDetectionService,
        ICaptchaService captchaService)
    {
        _context = context;
        _userRepository = userRepository;
        _mfaService = mfaService;
        _encryptionService = encryptionService;
        _backupCodeService = backupCodeService;
        _tokenService = tokenService;
        _deviceService = deviceService;
        _permissionCache = permissionCache;
        _forceReauthService = forceReauthService;
        _revokedSessionService = revokedSessionService;
        _auditService = auditService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _configuration = configuration;
        _riskScoringService = riskScoringService;
        _loginPatternService = loginPatternService;
        _geoIpService = geoIpService;
        _deviceDetectionService = deviceDetectionService;
        _captchaService = captchaService;
    }

    public async ValueTask<AuthResponse> Handle(MfaVerifyCommand command, CancellationToken ct)
    {
        // Check if CAPTCHA is required based on failed MFA attempts for this token
        var captchaRequired = await _captchaService.IsRequiredForMfaVerifyAsync(command.MfaToken, ct);
        await _captchaService.ValidateConditionalAsync(
            command.CaptchaToken,
            captchaRequired,
            "mfa_verify",
            command.IpAddress,
            ct);

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

                    await _auditService.LogWithContextAsync(
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
                        userId,
                        ct
                    );
                }
            }
        }

        if (!codeValid)
        {
            // Record failed attempt for CAPTCHA smart trigger
            await _captchaService.RecordFailedMfaAttemptAsync(command.MfaToken, ct);
            throw new MfaCodeInvalidException();
        }

        // Get permissions
        var permissions = await _permissionCache.GetOrSetPermissionsAsync(
            userId,
            () => _userRepository.GetUserPermissionNamesAsync(userId, ct),
            ct
        );

        // Get geo location and device info (needed for trust check and device creation)
        var geoLocation = _geoIpService.GetLocation(command.IpAddress);
        var deviceInfo = _deviceDetectionService.Parse(command.UserAgent);
        var deviceId = command.DeviceId ?? _deviceService.GenerateDeviceId();

        // Check if this device is trusted
        var device = await _deviceService.FindTrustedDeviceAsync(
            userId,
            deviceId,
            command.DeviceFingerprint,
            ct
        );

        // NEW DEVICE → Always require approval
        if (device is null)
        {
            // Calculate risk score for email/audit purposes
            var riskScore = await _riskScoringService.CalculateAsync(
                userId,
                deviceInfo,
                geoLocation,
                false, // Not trusted
                ct
            );

            // Create pending device with approval credentials
            var pendingResult = await _deviceService.CreatePendingDeviceAsync(
                userId,
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
                userId: userId,
                language: user.PreferredLanguage,
                cancellationToken: ct
            );

            // Audit log
            await _auditService.LogWithContextAsync(
                AuditActions.DeviceApprovalRequired,
                userId,
                null,
                "Device",
                pendingDevice.Id,
                new
                {
                    riskScore.Score,
                    riskScore.Level,
                    riskScore.Factors,
                    DeviceId = deviceId,
                    IsBackupCode = isBackupCode,
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
            userId,
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
                userId,
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
                userId: userId,
                language: user.PreferredLanguage,
                cancellationToken: ct
            );

            // Audit log
            await _auditService.LogWithContextAsync(
                AuditActions.DeviceApprovalRequired,
                userId,
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
                    IsBackupCode = isBackupCode,
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

        // Clear force re-auth flag and revoked session status for this device
        await _forceReauthService.ClearFlagAsync(device.Id, ct);
        await _revokedSessionService.ClearRevokedSessionAsync(device.Id, ct);

        // Generate tokens with device ID as session ID
        var accessToken = _tokenService.GenerateAccessToken(
            userId,
            user.Email,
            UserType.System,
            permissions,
            device.Id
        );

        var refreshTokenString = _tokenService.GenerateRefreshToken();
        var refreshToken = global::ExoAuth.Domain.Entities.RefreshToken.Create(
            userId: userId,
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
                SessionId = device.Id,
                DeviceId = deviceId,
                IsNewLocation = isNewLocation,
                IsBackupCode = isBackupCode,
                IsTrustedDevice = true
            },
            ct
        );

        // Send notification email for new location
        if (isNewLocation)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.LoginNewLocation,
                userId,
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
