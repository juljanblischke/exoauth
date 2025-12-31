using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Commands.RevokeUserSession;

public sealed class RevokeUserSessionHandler : ICommandHandler<RevokeUserSessionCommand, RevokeUserSessionResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;

    public RevokeUserSessionHandler(
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

    public async ValueTask<RevokeUserSessionResponse> Handle(RevokeUserSessionCommand command, CancellationToken ct)
    {
        var adminUserId = _currentUser.UserId
            ?? throw new UnauthorizedException();

        // Verify user exists
        var user = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
            ?? throw new SystemUserNotFoundException(command.UserId);

        // Get the specific session - must exist AND belong to the user
        var session = await _context.DeviceSessions
            .FirstOrDefaultAsync(s => s.Id == command.SessionId && s.UserId == command.UserId, ct)
            ?? throw new UserSessionNotFoundException(command.SessionId, command.UserId);

        // Mark session as revoked for immediate invalidation
        await _revokedSessionService.RevokeSessionAsync(session.Id, ct);

        // Revoke refresh token for this session
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == command.UserId && t.DeviceSessionId == command.SessionId && !t.IsRevoked, ct);

        if (refreshToken is not null)
        {
            refreshToken.Revoke();
        }

        // Remove the session
        _context.DeviceSessions.Remove(session);

        await _context.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            AuditActions.SessionRevokedByAdmin,
            adminUserId,
            command.UserId,
            "DeviceSession",
            command.SessionId,
            new
            {
                SessionId = command.SessionId,
                DeviceId = session.DeviceId,
                UserAgent = session.UserAgent
            },
            ct
        );

        // Send notification email to user (skip for anonymized users)
        if (!user.IsAnonymized)
        {
            await _emailService.SendAsync(
                user.Email,
                _emailTemplateService.GetSubject("session-revoked-admin", user.PreferredLanguage),
                "session-revoked-admin",
                new Dictionary<string, string>
                {
                    ["firstName"] = user.FirstName,
                    ["year"] = DateTime.UtcNow.Year.ToString()
                },
                user.PreferredLanguage,
                ct
            );
        }

        return new RevokeUserSessionResponse(true);
    }
}
