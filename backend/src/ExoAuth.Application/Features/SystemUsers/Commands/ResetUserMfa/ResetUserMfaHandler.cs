using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Commands.ResetUserMfa;

public sealed class ResetUserMfaHandler : ICommandHandler<ResetUserMfaCommand, ResetUserMfaResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly IForceReauthService _forceReauthService;
    private readonly ITokenBlacklistService _tokenBlacklistService;

    public ResetUserMfaHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IAuditService auditService,
        IEmailService emailService,
        IForceReauthService forceReauthService,
        ITokenBlacklistService tokenBlacklistService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditService = auditService;
        _emailService = emailService;
        _forceReauthService = forceReauthService;
        _tokenBlacklistService = tokenBlacklistService;
    }

    public async ValueTask<ResetUserMfaResponse> Handle(ResetUserMfaCommand command, CancellationToken ct)
    {
        var adminUserId = _currentUser.UserId
            ?? throw new UnauthorizedException();

        var user = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
            ?? throw new SystemUserNotFoundException(command.UserId);

        if (!user.MfaEnabled)
        {
            throw new MfaNotEnabledException();
        }

        // Disable MFA
        user.DisableMfa();

        // Delete all backup codes
        var backupCodes = await _context.MfaBackupCodes
            .Where(c => c.UserId == command.UserId)
            .ToListAsync(ct);

        if (backupCodes.Count > 0)
        {
            _context.MfaBackupCodes.RemoveRange(backupCodes);
        }

        // Force re-auth: user must login again after MFA reset
        await _forceReauthService.SetFlagAsync(command.UserId, ct);

        // Revoke all refresh tokens for this user
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == command.UserId && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.Revoke();
            await _tokenBlacklistService.BlacklistAsync(token.Id, token.ExpiresAt, ct);
        }

        await _context.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            AuditActions.MfaResetByAdmin,
            adminUserId,
            command.UserId,
            "SystemUser",
            command.UserId,
            new { Reason = command.Reason },
            ct
        );

        // Send notification email to user
        var subject = user.PreferredLanguage.StartsWith("de")
            ? "Ihre Zwei-Faktor-Authentifizierung wurde zurückgesetzt"
            : "Your Two-Factor Authentication Has Been Reset";

        var defaultReason = user.PreferredLanguage.StartsWith("de")
            ? "Ein Administrator hat Ihre MFA zurückgesetzt."
            : "An administrator reset your MFA.";

        await _emailService.SendAsync(
            user.Email,
            subject,
            "mfa-reset-admin",
            new Dictionary<string, string>
            {
                ["firstName"] = user.FirstName,
                ["reason"] = command.Reason ?? defaultReason
            },
            user.PreferredLanguage,
            ct
        );

        return new ResetUserMfaResponse(true);
    }
}
