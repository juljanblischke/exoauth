using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Commands.AnonymizeUser;

public sealed class AnonymizeUserHandler : ICommandHandler<AnonymizeUserCommand, AnonymizeUserResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly IAuditService _auditService;
    private readonly ISystemUserRepository _userRepository;

    public AnonymizeUserHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IRevokedSessionService revokedSessionService,
        IAuditService auditService,
        ISystemUserRepository userRepository)
    {
        _context = context;
        _currentUser = currentUser;
        _revokedSessionService = revokedSessionService;
        _auditService = auditService;
        _userRepository = userRepository;
    }

    public async ValueTask<AnonymizeUserResponse> Handle(AnonymizeUserCommand command, CancellationToken ct)
    {
        var adminUserId = _currentUser.UserId
            ?? throw new UnauthorizedException();

        // Cannot anonymize yourself
        if (adminUserId == command.UserId)
        {
            throw new CannotDeleteSelfException();
        }

        var user = await _context.SystemUsers
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
            ?? throw new SystemUserNotFoundException(command.UserId);

        if (user.IsAnonymized)
        {
            // Already anonymized
            return new AnonymizeUserResponse(true, command.UserId);
        }

        // Check if user is last holder of critical permission (system:users:update)
        var userPermissionNames = await _userRepository.GetUserPermissionNamesAsync(command.UserId, ct);
        if (userPermissionNames.Contains(global::ExoAuth.Domain.Constants.SystemPermissions.UsersUpdate))
        {
            var holdersCount = await _userRepository.CountUsersWithPermissionAsync(
                global::ExoAuth.Domain.Constants.SystemPermissions.UsersUpdate, ct);

            if (holdersCount <= 1)
            {
                throw new LastPermissionHolderException(global::ExoAuth.Domain.Constants.SystemPermissions.UsersUpdate);
            }
        }

        // Store original email for audit before anonymization
        var originalEmail = user.Email;

        // Get all sessions for this user
        var sessions = await _context.DeviceSessions
            .Where(s => s.UserId == command.UserId)
            .ToListAsync(ct);

        // Mark sessions as revoked for immediate invalidation
        foreach (var session in sessions)
        {
            await _revokedSessionService.RevokeSessionAsync(session.Id, ct);
        }

        // Revoke all refresh tokens
        var refreshTokens = await _context.RefreshTokens
            .Where(t => t.UserId == command.UserId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in refreshTokens)
        {
            token.Revoke();
        }

        // Remove all sessions
        _context.DeviceSessions.RemoveRange(sessions);

        // Delete all backup codes
        var backupCodes = await _context.MfaBackupCodes
            .Where(c => c.UserId == command.UserId)
            .ToListAsync(ct);

        if (backupCodes.Count > 0)
        {
            _context.MfaBackupCodes.RemoveRange(backupCodes);
        }

        // Remove all permissions
        var permissions = user.Permissions.ToList();
        _context.SystemUserPermissions.RemoveRange(permissions);

        // Anonymize user data (GDPR compliant)
        user.Anonymize();

        await _context.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            AuditActions.UserAnonymized,
            adminUserId,
            command.UserId,
            "SystemUser",
            command.UserId,
            new { OriginalEmail = originalEmail },
            ct
        );

        return new AnonymizeUserResponse(true, command.UserId);
    }
}
