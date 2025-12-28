using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Entities;
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
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly int _backupCodeCount;

    public MfaConfirmHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IMfaService mfaService,
        IEncryptionService encryptionService,
        IBackupCodeService backupCodeService,
        IAuditService auditService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _context = context;
        _currentUser = currentUser;
        _mfaService = mfaService;
        _encryptionService = encryptionService;
        _backupCodeService = backupCodeService;
        _auditService = auditService;
        _emailService = emailService;
        _backupCodeCount = configuration.GetValue("Mfa:BackupCodeCount", 10);
    }

    public async ValueTask<MfaConfirmResponse> Handle(MfaConfirmCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException();

        var user = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException();

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
        await _emailService.SendAsync(
            user.Email,
            "Two-Factor Authentication Enabled",
            "mfa-enabled",
            new Dictionary<string, string>
            {
                ["firstName"] = user.FirstName
            },
            user.PreferredLanguage,
            ct
        );

        return new MfaConfirmResponse(true, backupCodes);
    }
}
