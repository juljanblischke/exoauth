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
    private readonly IDeviceService _deviceService;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;

    public RevokeUserSessionHandler(
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

    public async ValueTask<RevokeUserSessionResponse> Handle(RevokeUserSessionCommand command, CancellationToken ct)
    {
        var adminUserId = _currentUser.UserId
            ?? throw new UnauthorizedException();

        // Verify user exists
        var user = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
            ?? throw new SystemUserNotFoundException(command.UserId);

        // Get the specific device (session) - must exist AND belong to the user
        var device = await _deviceService.GetByIdAsync(command.SessionId, ct);

        if (device is null || device.UserId != command.UserId)
        {
            throw new UserSessionNotFoundException(command.SessionId, command.UserId);
        }

        // Mark session as revoked for immediate invalidation
        await _revokedSessionService.RevokeSessionAsync(device.Id, ct);

        // Revoke the device (which also revokes associated refresh tokens)
        await _deviceService.RevokeAsync(device.Id, ct);

        await _context.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            AuditActions.SessionRevokedByAdmin,
            adminUserId,
            command.UserId,
            "Device",
            command.SessionId,
            new
            {
                DeviceId = device.DeviceId,
                DisplayName = device.DisplayName
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
