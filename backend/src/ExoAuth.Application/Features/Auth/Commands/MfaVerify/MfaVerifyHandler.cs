using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

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
        IEmailService emailService)
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
                        "Backup Code Used",
                        "mfa-backup-code-used",
                        new Dictionary<string, string>
                        {
                            ["firstName"] = user.FirstName,
                            ["remainingCodes"] = remainingCodes.ToString()
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

        // Clear force re-auth flag if set
        await _forceReauthService.ClearFlagAsync(userId, ct);

        // Get permissions
        var permissions = await _permissionCache.GetOrSetPermissionsAsync(
            userId,
            () => _userRepository.GetUserPermissionNamesAsync(userId, ct),
            ct
        );

        // Create or update device session
        var deviceId = command.DeviceId ?? _deviceSessionService.GenerateDeviceId();
        var (deviceSession, isNewDevice, isNewLocation) = await _deviceSessionService.CreateOrUpdateSessionAsync(
            userId,
            deviceId,
            command.UserAgent,
            command.IpAddress,
            command.DeviceFingerprint,
            ct
        );

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
                IsBackupCode = isBackupCode
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
            SessionId: deviceSession.Id,
            DeviceId: deviceId,
            IsNewDevice: isNewDevice,
            IsNewLocation: isNewLocation
        );
    }
}
