using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.MfaDisable;

public sealed class MfaDisableHandler : ICommandHandler<MfaDisableCommand, MfaDisableResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMfaService _mfaService;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;

    public MfaDisableHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IMfaService mfaService,
        IEncryptionService encryptionService,
        IAuditService auditService,
        IEmailService emailService)
    {
        _context = context;
        _currentUser = currentUser;
        _mfaService = mfaService;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _emailService = emailService;
    }

    public async ValueTask<MfaDisableResponse> Handle(MfaDisableCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException();

        var user = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException();

        // Check user state
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

        // Disable MFA
        user.DisableMfa();

        // Delete all backup codes
        var backupCodes = await _context.MfaBackupCodes
            .Where(c => c.UserId == userId)
            .ToListAsync(ct);

        if (backupCodes.Any())
        {
            _context.MfaBackupCodes.RemoveRange(backupCodes);
        }

        await _context.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            AuditActions.MfaDisabled,
            userId,
            null,
            "SystemUser",
            userId,
            null,
            ct
        );

        // Send notification email
        var subject = user.PreferredLanguage.StartsWith("de")
            ? "Zwei-Faktor-Authentifizierung deaktiviert"
            : "Two-Factor Authentication Disabled";

        await _emailService.SendAsync(
            user.Email,
            subject,
            "mfa-disabled",
            new Dictionary<string, string>
            {
                ["firstName"] = user.FirstName
            },
            user.PreferredLanguage,
            ct
        );

        return new MfaDisableResponse(true);
    }
}
