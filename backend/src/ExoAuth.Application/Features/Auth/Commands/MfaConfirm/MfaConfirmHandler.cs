using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExoAuth.Application.Features.Auth.Commands.MfaConfirm;

public sealed class MfaConfirmHandler : ICommandHandler<MfaConfirmCommand, MfaConfirmResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMfaService _mfaService;
    private readonly IEncryptionService _encryptionService;
    private readonly IBackupCodeService _backupCodeService;
    private readonly ITokenService _tokenService;
    private readonly IDeviceSessionService _deviceSessionService;
    private readonly ISystemUserRepository _userRepository;
    private readonly IPermissionCacheService _permissionCache;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly int _backupCodeCount;

    public MfaConfirmHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IMfaService mfaService,
        IEncryptionService encryptionService,
        IBackupCodeService backupCodeService,
        ITokenService tokenService,
        IDeviceSessionService deviceSessionService,
        ISystemUserRepository userRepository,
        IPermissionCacheService permissionCache,
        IAuditService auditService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _context = context;
        _currentUser = currentUser;
        _mfaService = mfaService;
        _encryptionService = encryptionService;
        _backupCodeService = backupCodeService;
        _tokenService = tokenService;
        _deviceSessionService = deviceSessionService;
        _userRepository = userRepository;
        _permissionCache = permissionCache;
        _auditService = auditService;
        _emailService = emailService;
        _backupCodeCount = configuration.GetValue("Mfa:BackupCodeCount", 10);
    }

    public async ValueTask<MfaConfirmResponse> Handle(MfaConfirmCommand command, CancellationToken ct)
    {
        // Dual-mode authentication:
        // 1. JWT auth: User already logged in (from settings page) - returns only backup codes
        // 2. SetupToken: Forced MFA setup during login/registration flow - returns tokens + backup codes
        Guid userId;
        bool isForcedSetupFlow = false;

        if (_currentUser.UserId.HasValue)
        {
            // Case 1: Already authenticated via JWT
            userId = _currentUser.UserId.Value;
        }
        else if (!string.IsNullOrEmpty(command.SetupToken))
        {
            // Case 2: Forced setup during login/registration (using setupToken)
            var tokenData = _mfaService.ValidateMfaToken(command.SetupToken)
                ?? throw new MfaTokenInvalidException();
            userId = tokenData.userId;
            isForcedSetupFlow = true;
        }
        else
        {
            throw new UnauthorizedException();
        }

        var user = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException();

        if (!user.IsActive)
        {
            throw new UserInactiveException();
        }

        if (user.IsLocked)
        {
            throw new AccountLockedException(user.LockedUntil);
        }

        if (user.MfaEnabled)
        {
            throw new MfaAlreadyEnabledException();
        }

        if (string.IsNullOrEmpty(user.MfaSecret))
        {
            throw new MfaNotEnabledException();
        }

        // Decrypt secret and validate code
        var secret = _encryptionService.Decrypt(user.MfaSecret);

        if (!_mfaService.ValidateCode(secret, command.Code))
        {
            throw new MfaCodeInvalidException();
        }

        // Enable MFA
        user.EnableMfa();

        // Generate backup codes
        var backupCodes = _backupCodeService.GenerateCodes(_backupCodeCount);

        // Delete any existing backup codes
        var existingCodes = await _context.MfaBackupCodes
            .Where(c => c.UserId == userId)
            .ToListAsync(ct);

        if (existingCodes.Any())
        {
            _context.MfaBackupCodes.RemoveRange(existingCodes);
        }

        // Store hashed backup codes
        foreach (var code in backupCodes)
        {
            var hashedCode = _backupCodeService.HashCode(code);
            var backupCodeEntity = MfaBackupCode.Create(userId, hashedCode);
            await _context.MfaBackupCodes.AddAsync(backupCodeEntity, ct);
        }

        await _context.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            AuditActions.MfaEnabled,
            userId,
            null,
            "SystemUser",
            userId,
            new { BackupCodesGenerated = _backupCodeCount },
            ct
        );

        // Send notification email
        var subject = user.PreferredLanguage.StartsWith("de")
            ? "Zwei-Faktor-Authentifizierung aktiviert"
            : "Two-Factor Authentication Enabled";

        await _emailService.SendAsync(
            user.Email,
            subject,
            "mfa-enabled",
            new Dictionary<string, string>
            {
                ["firstName"] = user.FirstName
            },
            user.PreferredLanguage,
            ct
        );

        // For forced setup flow (registration/login), create session and return tokens
        if (isForcedSetupFlow)
        {
            // Get permissions
            var permissions = await _permissionCache.GetOrSetPermissionsAsync(
                userId,
                () => _userRepository.GetUserPermissionNamesAsync(userId, ct),
                ct
            );

            // Create device session
            var deviceId = command.DeviceId ?? _deviceSessionService.GenerateDeviceId();
            var (session, _, _) = await _deviceSessionService.CreateOrUpdateSessionAsync(
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
                session.Id
            );

            var refreshTokenString = _tokenService.GenerateRefreshToken();
            var refreshToken = global::ExoAuth.Domain.Entities.RefreshToken.Create(
                userId: userId,
                userType: UserType.System,
                token: refreshTokenString,
                expirationDays: (int)_tokenService.RefreshTokenExpiration.TotalDays
            );

            refreshToken.LinkToSession(session.Id);

            await _context.RefreshTokens.AddAsync(refreshToken, ct);
            await _context.SaveChangesAsync(ct);

            // Record login
            user.RecordLogin();
            await _userRepository.UpdateAsync(user, ct);

            // Audit log for completed registration/login
            await _auditService.LogWithContextAsync(
                AuditActions.MfaSetupCompleted,
                userId,
                null,
                "SystemUser",
                userId,
                new { SessionId = session.Id, DeviceId = deviceId },
                ct
            );

            return new MfaConfirmResponse(
                Success: true,
                BackupCodes: backupCodes,
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
                SessionId: session.Id,
                DeviceId: deviceId
            );
        }

        // For settings flow (already authenticated), just return backup codes
        return new MfaConfirmResponse(true, backupCodes);
    }
}
