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
    private readonly IDeviceService _deviceService;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;

    public RevokeUserSessionsHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IDeviceService deviceService,
        IRevokedSessionService revokedSessionService,
        IAuditService auditService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService)
    {
        _context = context;
        _currentUser = currentUser;
        _deviceService = deviceService;
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

        // Get all devices (sessions) for this user
        var devices = await _deviceService.GetAllForUserAsync(command.UserId, ct);

        var revokedCount = devices.Count;

        if (revokedCount == 0)
        {
            return new RevokeUserSessionsResponse(0);
        }

        // Mark sessions as revoked for immediate invalidation
        foreach (var device in devices)
        {
            await _revokedSessionService.RevokeSessionAsync(device.Id, ct);
        }

        // Remove all devices (which also revokes associated refresh tokens)
        await _deviceService.RemoveAllAsync(command.UserId, ct);

        await _context.SaveChangesAsync(ct);

        // Audit log (use plural - sessions)
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
