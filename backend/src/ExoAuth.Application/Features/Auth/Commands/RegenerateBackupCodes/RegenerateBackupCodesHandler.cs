using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExoAuth.Application.Features.Auth.Commands.RegenerateBackupCodes;

public sealed class RegenerateBackupCodesHandler : ICommandHandler<RegenerateBackupCodesCommand, RegenerateBackupCodesResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMfaService _mfaService;
    private readonly IEncryptionService _encryptionService;
    private readonly IBackupCodeService _backupCodeService;
    private readonly IAuditService _auditService;
    private readonly int _backupCodeCount;

    public RegenerateBackupCodesHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IMfaService mfaService,
        IEncryptionService encryptionService,
        IBackupCodeService backupCodeService,
        IAuditService auditService,
        IConfiguration configuration)
    {
        _context = context;
        _currentUser = currentUser;
        _mfaService = mfaService;
        _encryptionService = encryptionService;
        _backupCodeService = backupCodeService;
        _auditService = auditService;
        _backupCodeCount = configuration.GetValue("Mfa:BackupCodeCount", 10);
    }

    public async ValueTask<RegenerateBackupCodesResponse> Handle(RegenerateBackupCodesCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException();

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

        if (!user.MfaEnabled || string.IsNullOrEmpty(user.MfaSecret))
        {
            throw new MfaNotEnabledException();
        }

        // Verify TOTP code
        var secret = _encryptionService.Decrypt(user.MfaSecret);

        if (!_mfaService.ValidateCode(secret, command.Code))
        {
            throw new MfaCodeInvalidException();
        }

        // Delete existing backup codes
        var existingCodes = await _context.MfaBackupCodes
            .Where(c => c.UserId == userId)
            .ToListAsync(ct);

        if (existingCodes.Any())
        {
            _context.MfaBackupCodes.RemoveRange(existingCodes);
        }

        // Generate new backup codes
        var backupCodes = _backupCodeService.GenerateCodes(_backupCodeCount);

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
            AuditActions.MfaBackupCodesRegenerated,
            userId,
            null,
            "SystemUser",
            userId,
            new { CodesGenerated = _backupCodeCount },
            ct
        );

        return new RegenerateBackupCodesResponse(backupCodes);
    }
}
