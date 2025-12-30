using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Commands.RevokeUserSessions;

public sealed class RevokeUserSessionsHandler : ICommandHandler<RevokeUserSessionsCommand, RevokeUserSessionsResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;

    public RevokeUserSessionsHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IRevokedSessionService revokedSessionService,
        IAuditService auditService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService)
    {
        _context = context;
        _currentUser = currentUser;
        _revokedSessionService = revokedSessionService;
        _auditService = auditService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
    }

    public async ValueTask<RevokeUserSessionsResponse> Handle(RevokeUserSessionsCommand command, CancellationToken ct)
    {
        var adminUserId = _currentUser.UserId
            ?? throw new UnauthorizedException();

        var user = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
            ?? throw new SystemUserNotFoundException(command.UserId);

        // Get all sessions for this user
        var sessions = await _context.DeviceSessions
            .Where(s => s.UserId == command.UserId)
            .ToListAsync(ct);

        var revokedCount = sessions.Count;

        if (revokedCount == 0)
        {
            return new RevokeUserSessionsResponse(0);
        }

        // Mark sessions as revoked for immediate invalidation
        foreach (var session in sessions)
        {
            await _revokedSessionService.RevokeSessionAsync(session.Id, ct);
        }

        // Revoke all refresh tokens for this user
        var refreshTokens = await _context.RefreshTokens
            .Where(t => t.UserId == command.UserId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in refreshTokens)
        {
            token.Revoke();
        }

        // Remove all sessions
        _context.DeviceSessions.RemoveRange(sessions);

        await _context.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            AuditActions.SessionsRevokedByAdmin,
            adminUserId,
            command.UserId,
            "SystemUser",
            command.UserId,
            new { RevokedCount = revokedCount },
            ct
        );

        // Send notification email to user (skip for anonymized users)
        if (!user.IsAnonymized)
        {
            await _emailService.SendAsync(
                user.Email,
                _emailTemplateService.GetSubject("sessions-revoked-admin", user.PreferredLanguage),
                "sessions-revoked-admin",
                new Dictionary<string, string>
                {
                    ["firstName"] = user.FirstName,
                    ["sessionCount"] = revokedCount.ToString(),
                    ["year"] = DateTime.UtcNow.Year.ToString()
                },
                user.PreferredLanguage,
                ct
            );
        }

        return new RevokeUserSessionsResponse(revokedCount);
    }
}
