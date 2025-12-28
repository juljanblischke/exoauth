using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RevokeSession;

public sealed class RevokeSessionHandler : ICommandHandler<RevokeSessionCommand, RevokeSessionResponse>
{
    private readonly IDeviceSessionService _sessionService;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public RevokeSessionHandler(
        IDeviceSessionService sessionService,
        IRevokedSessionService revokedSessionService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _sessionService = sessionService;
        _revokedSessionService = revokedSessionService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async ValueTask<RevokeSessionResponse> Handle(RevokeSessionCommand command, CancellationToken ct)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException();

        var currentSessionId = _currentUserService.SessionId;

        // Check if trying to revoke current session
        if (command.SessionId == currentSessionId)
        {
            throw new CannotRevokeCurrentSessionException();
        }

        // Get the session and verify ownership
        var session = await _sessionService.GetSessionByIdAsync(command.SessionId, ct);

        if (session is null || session.UserId != userId)
        {
            throw new SessionNotFoundException();
        }

        var revoked = await _sessionService.RevokeSessionAsync(command.SessionId, ct);

        if (revoked)
        {
            // Immediately invalidate access tokens for this session
            await _revokedSessionService.RevokeSessionAsync(command.SessionId, ct);

            await _auditService.LogWithContextAsync(
                AuditActions.SessionRevoked,
                userId,
                null,
                "DeviceSession",
                command.SessionId,
                new { DeviceId = session.DeviceId, DeviceName = session.DisplayName },
                ct
            );
        }

        return new RevokeSessionResponse(revoked);
    }
}
