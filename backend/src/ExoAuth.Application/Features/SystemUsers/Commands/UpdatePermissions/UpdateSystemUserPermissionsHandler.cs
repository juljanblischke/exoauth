using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Commands.UpdatePermissions;

public sealed class UpdateSystemUserPermissionsHandler : ICommandHandler<UpdateSystemUserPermissionsCommand, SystemUserDetailDto>
{
    private readonly IAppDbContext _context;
    private readonly ISystemUserRepository _userRepository;
    private readonly IPermissionCacheService _permissionCache;
    private readonly IForceReauthService _forceReauthService;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public UpdateSystemUserPermissionsHandler(
        IAppDbContext context,
        ISystemUserRepository userRepository,
        IPermissionCacheService permissionCache,
        IForceReauthService forceReauthService,
        ITokenBlacklistService tokenBlacklistService,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _context = context;
        _userRepository = userRepository;
        _permissionCache = permissionCache;
        _forceReauthService = forceReauthService;
        _tokenBlacklistService = tokenBlacklistService;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async ValueTask<SystemUserDetailDto> Handle(UpdateSystemUserPermissionsCommand command, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdWithPermissionsAsync(command.UserId, ct);

        if (user is null)
        {
            throw new SystemUserNotFoundException(command.UserId);
        }

        // Cannot modify anonymized users
        if (user.IsAnonymized)
        {
            throw new UserAnonymizedException(command.UserId);
        }

        // Validate permissions exist
        var validPermissions = await _context.SystemPermissions
            .Where(p => command.PermissionIds.Contains(p.Id))
            .ToListAsync(ct);

        var validPermissionIds = validPermissions.Select(p => p.Id).ToList();
        var invalidPermissions = command.PermissionIds.Except(validPermissionIds).ToList();

        if (invalidPermissions.Count > 0)
        {
            throw new SystemPermissionNotFoundException(invalidPermissions.First());
        }

        // Get current permissions
        var currentPermissionNames = user.Permissions.Select(p => p.SystemPermission.Name).ToList();
        var newPermissionNames = validPermissions.Select(p => p.Name).ToList();

        // Check for critical permission removal
        var removedPermissions = currentPermissionNames.Except(newPermissionNames).ToList();

        // Check if user is the last holder of system:users:update permission
        if (removedPermissions.Contains(global::ExoAuth.Domain.Constants.SystemPermissions.UsersUpdate))
        {
            var holdersCount = await _userRepository.CountUsersWithPermissionAsync(global::ExoAuth.Domain.Constants.SystemPermissions.UsersUpdate, ct);

            if (holdersCount <= 1)
            {
                throw new LastPermissionHolderException(global::ExoAuth.Domain.Constants.SystemPermissions.UsersUpdate);
            }
        }

        // Update permissions
        await _userRepository.SetUserPermissionsAsync(
            command.UserId,
            validPermissionIds,
            _currentUser.UserId,
            ct
        );

        // Invalidate permission cache
        await _permissionCache.InvalidateAsync(command.UserId, ct);

        // Force re-auth: Set flag for ALL sessions of this user (session-based reauth)
        await _forceReauthService.SetFlagForAllSessionsAsync(command.UserId, ct);

        // Revoke all refresh tokens for this user
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == command.UserId && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.Revoke();
            await _tokenBlacklistService.BlacklistAsync(token.Id, token.ExpiresAt, ct);
        }

        await _context.SaveChangesAsync(ct);

        // Reload user with new permissions
        user = await _userRepository.GetByIdWithPermissionsAsync(command.UserId, ct);

        var addedPermissions = newPermissionNames.Except(currentPermissionNames).ToList();

        // Audit log with target user
        await _auditService.LogWithContextAsync(
            AuditActions.UserPermissionsUpdated,
            _currentUser.UserId,
            command.UserId, // targetUserId
            "SystemUser",
            command.UserId,
            new { Added = addedPermissions, Removed = removedPermissions },
            ct
        );

        var permissions = user!.Permissions.Select(p => new PermissionDto(
            Id: p.SystemPermission.Id,
            Name: p.SystemPermission.Name,
            Description: p.SystemPermission.Description,
            Category: p.SystemPermission.Category
        )).ToList();

        return new SystemUserDetailDto(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            FullName: user.FullName,
            IsActive: user.IsActive,
            EmailVerified: user.EmailVerified,
            MfaEnabled: user.MfaEnabled,
            MfaEnabledAt: user.MfaEnabledAt,
            PreferredLanguage: user.PreferredLanguage,
            IsLocked: user.IsLocked,
            LockedUntil: user.LockedUntil,
            FailedLoginAttempts: user.FailedLoginAttempts,
            IsAnonymized: user.IsAnonymized,
            AnonymizedAt: user.AnonymizedAt,
            LastLoginAt: user.LastLoginAt,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt,
            Permissions: permissions
        );
    }
}
