using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.Logout;

public sealed class LogoutHandler : ICommandHandler<LogoutCommand, LogoutResponse>
{
    private readonly IAppDbContext _context;
    private readonly ITokenBlacklistService _tokenBlacklist;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public LogoutHandler(
        IAppDbContext context,
        ITokenBlacklistService tokenBlacklist,
        IRevokedSessionService revokedSessionService,
        IAuditService auditService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _tokenBlacklist = tokenBlacklist;
        _revokedSessionService = revokedSessionService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    public async ValueTask<LogoutResponse> Handle(LogoutCommand command, CancellationToken ct)
    {
        // Find the refresh token
        var tokens = await _context.RefreshTokens
            .Where(t => !t.IsRevoked)
            .ToListAsync(ct);

        var storedToken = tokens.FirstOrDefault(t => t.ValidateToken(command.RefreshToken));

        if (storedToken is not null)
        {
            // Revoke the token
            storedToken.Revoke();
            await _tokenBlacklist.BlacklistAsync(storedToken.Id, storedToken.ExpiresAt, ct);

            // Also revoke the linked device session for immediate access token invalidation
            if (storedToken.DeviceSessionId.HasValue)
            {
                await _revokedSessionService.RevokeSessionAsync(storedToken.DeviceSessionId.Value, ct);

                // Remove the device session from DB
                var session = await _context.DeviceSessions
                    .FirstOrDefaultAsync(s => s.Id == storedToken.DeviceSessionId.Value, ct);
                if (session is not null)
                {
                    _context.DeviceSessions.Remove(session);
                }
            }

            await _context.SaveChangesAsync(ct);

            // Audit log
            await _auditService.LogWithContextAsync(
                AuditActions.UserLogout,
                storedToken.UserId,
                null, // targetUserId
                "SystemUser",
                storedToken.UserId,
                null,
                ct
            );
        }

        // Always return success (don't reveal if token existed)
        return new LogoutResponse(Success: true);
    }
}
