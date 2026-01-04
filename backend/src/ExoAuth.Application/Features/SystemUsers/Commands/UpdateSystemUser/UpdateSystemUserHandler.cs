using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Commands.UpdateSystemUser;

public sealed class UpdateSystemUserHandler : ICommandHandler<UpdateSystemUserCommand, SystemUserDto>
{
    private readonly ISystemUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;
    private readonly IAppDbContext _context;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly IPermissionCacheService _permissionCache;
    private readonly IDeviceService _deviceService;

    public UpdateSystemUserHandler(
        ISystemUserRepository userRepository,
        ICurrentUserService currentUser,
        IAuditService auditService,
        IAppDbContext context,
        IRevokedSessionService revokedSessionService,
        IPermissionCacheService permissionCache,
        IDeviceService deviceService)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _auditService = auditService;
        _context = context;
        _revokedSessionService = revokedSessionService;
        _permissionCache = permissionCache;
        _deviceService = deviceService;
    }

    public async ValueTask<SystemUserDto> Handle(UpdateSystemUserCommand command, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(command.Id, ct);

        if (user is null)
        {
            throw new SystemUserNotFoundException(command.Id);
        }

        // Cannot modify anonymized users
        if (user.IsAnonymized)
        {
            throw new UserAnonymizedException(command.Id);
        }

        // If deactivating via Update, apply the same safeguards as DeactivateSystemUserHandler
        if (command.IsActive == false && user.IsActive)
        {
            // Check if trying to deactivate self
            if (_currentUser.UserId == command.Id)
            {
                throw new CannotDeleteSelfException();
            }

            // Check if user is last holder of critical permission
            var userPermissions = await _userRepository.GetUserPermissionNamesAsync(command.Id, ct);

            if (userPermissions.Contains(global::ExoAuth.Domain.Constants.SystemPermissions.UsersUpdate))
            {
                var holdersCount = await _userRepository.CountUsersWithPermissionAsync(
                    global::ExoAuth.Domain.Constants.SystemPermissions.UsersUpdate, ct);

                if (holdersCount <= 1)
                {
                    throw new LastPermissionHolderException(global::ExoAuth.Domain.Constants.SystemPermissions.UsersUpdate);
                }
            }
        }

        var changes = new Dictionary<string, object?>();

        if (command.FirstName is not null && command.FirstName != user.FirstName)
        {
            changes["FirstName"] = new { Old = user.FirstName, New = command.FirstName };
        }

        if (command.LastName is not null && command.LastName != user.LastName)
        {
            changes["LastName"] = new { Old = user.LastName, New = command.LastName };
        }

        if (command.IsActive.HasValue && command.IsActive.Value != user.IsActive)
        {
            changes["IsActive"] = new { Old = user.IsActive, New = command.IsActive.Value };
        }

        // Track if we're deactivating for post-update actions
        var isDeactivating = command.IsActive == false && user.IsActive;

        // Update user
        user.Update(
            firstName: command.FirstName,
            lastName: command.LastName,
            isActive: command.IsActive
        );

        await _userRepository.UpdateAsync(user, ct);

        // If deactivating, revoke all devices and tokens for immediate logout
        if (isDeactivating)
        {
            // Revoke all devices (sessions) for immediate access token invalidation
            var devices = await _deviceService.GetAllForUserAsync(command.Id, ct);

            foreach (var device in devices)
            {
                await _revokedSessionService.RevokeSessionAsync(device.Id, ct);
            }

            await _deviceService.RemoveAllAsync(command.Id, ct);

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
        }

        // Audit log
        if (changes.Count > 0)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.UserUpdated,
                _currentUser.UserId,
                user.Id, // targetUserId
                "SystemUser",
                user.Id,
                new { Changes = changes },
                ct
            );
        }

        return new SystemUserDto(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            FullName: user.FullName,
            IsActive: user.IsActive,
            EmailVerified: user.EmailVerified,
            MfaEnabled: user.MfaEnabled,
            IsLocked: user.IsLocked,
            IsAnonymized: user.IsAnonymized,
            LastLoginAt: user.LastLoginAt,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt
        );
    }
}
