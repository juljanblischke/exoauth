using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Commands.DeactivateSystemUser;

public sealed class DeactivateSystemUserHandler : ICommandHandler<DeactivateSystemUserCommand, bool>
{
    private readonly IAppDbContext _context;
    private readonly ISystemUserRepository _userRepository;
    private readonly IPermissionCacheService _permissionCache;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;
    private readonly IRevokedSessionService _revokedSessionService;

    public DeactivateSystemUserHandler(
        IAppDbContext context,
        ISystemUserRepository userRepository,
        IPermissionCacheService permissionCache,
        ICurrentUserService currentUser,
        IAuditService auditService,
        IRevokedSessionService revokedSessionService)
    {
        _context = context;
        _userRepository = userRepository;
        _permissionCache = permissionCache;
        _currentUser = currentUser;
        _auditService = auditService;
        _revokedSessionService = revokedSessionService;
    }

    public async ValueTask<bool> Handle(DeactivateSystemUserCommand command, CancellationToken ct)
    {
        // Check if trying to deactivate self
        if (_currentUser.UserId == command.Id)
        {
            throw new CannotDeleteSelfException();
        }

        var user = await _userRepository.GetByIdAsync(command.Id, ct);

        if (user is null)
        {
            throw new SystemUserNotFoundException(command.Id);
        }

        if (!user.IsActive)
        {
            throw new UserAlreadyDeactivatedException(command.Id);
        }

        // Check if user is last holder of critical permission
        var userPermissions = await _userRepository.GetUserPermissionNamesAsync(command.Id, ct);

        if (userPermissions.Contains(global::ExoAuth.Domain.Constants.SystemPermissions.UsersUpdate))
        {
            var holdersCount = await _userRepository.CountUsersWithPermissionAsync(global::ExoAuth.Domain.Constants.SystemPermissions.UsersUpdate, ct);

            if (holdersCount <= 1)
            {
                throw new LastPermissionHolderException(global::ExoAuth.Domain.Constants.SystemPermissions.UsersUpdate);
            }
        }

        // Deactivate user (soft delete)
        user.Deactivate();
        await _userRepository.UpdateAsync(user, ct);

        // Revoke all sessions for immediate logout
        var sessions = await _context.DeviceSessions
            .Where(s => s.UserId == command.Id)
            .ToListAsync(ct);

        foreach (var session in sessions)
        {
            await _revokedSessionService.RevokeSessionAsync(session.Id, ct);
        }

        _context.DeviceSessions.RemoveRange(sessions);

        // Revoke all refresh tokens
        var refreshTokens = await _context.RefreshTokens
            .Where(t => t.UserId == command.Id && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in refreshTokens)
        {
            token.Revoke();
        }

        await _context.SaveChangesAsync(ct);

        // Invalidate permission cache
        await _permissionCache.InvalidateAsync(command.Id, ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.UserDeactivated,
            _currentUser.UserId,
            command.Id, // targetUserId
            "SystemUser",
            command.Id,
            null,
            ct
        );

        return true;
    }
}
