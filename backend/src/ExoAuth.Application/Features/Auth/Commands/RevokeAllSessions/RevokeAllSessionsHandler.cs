using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RevokeAllSessions;

public sealed class RevokeAllSessionsHandler : ICommandHandler<RevokeAllSessionsCommand, RevokeAllSessionsResponse>
{
    private readonly IAppDbContext _context;
    private readonly IDeviceSessionService _sessionService;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public RevokeAllSessionsHandler(
        IAppDbContext context,
        IDeviceSessionService sessionService,
        IRevokedSessionService revokedSessionService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _context = context;
        _sessionService = sessionService;
        _revokedSessionService = revokedSessionService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async ValueTask<RevokeAllSessionsResponse> Handle(RevokeAllSessionsCommand command, CancellationToken ct)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException();

        var currentSessionId = _currentUserService.SessionId;

        // Get session IDs before revoking so we can invalidate access tokens
        var sessionIdsToRevoke = await _context.DeviceSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.Id != currentSessionId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var revokedCount = await _sessionService.RevokeAllSessionsExceptAsync(userId, currentSessionId, ct);

        if (revokedCount > 0)
        {
            // Immediately invalidate access tokens for all revoked sessions
            await _revokedSessionService.RevokeSessionsAsync(sessionIdsToRevoke, ct);

            await _auditService.LogWithContextAsync(
                AuditActions.SessionRevokedAll,
                userId,
                null,
                "DeviceSession",
                null,
                new { RevokedCount = revokedCount, ExceptSessionId = currentSessionId },
                ct
            );
        }

        return new RevokeAllSessionsResponse(revokedCount);
    }
}
