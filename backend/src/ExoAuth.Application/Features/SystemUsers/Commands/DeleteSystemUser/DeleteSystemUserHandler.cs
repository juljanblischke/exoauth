using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.DeleteSystemUser;

public sealed class DeleteSystemUserHandler : ICommandHandler<DeleteSystemUserCommand, bool>
{
    private readonly ISystemUserRepository _userRepository;
    private readonly IPermissionCacheService _permissionCache;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public DeleteSystemUserHandler(
        ISystemUserRepository userRepository,
        IPermissionCacheService permissionCache,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _userRepository = userRepository;
        _permissionCache = permissionCache;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async ValueTask<bool> Handle(DeleteSystemUserCommand command, CancellationToken ct)
    {
        // Check if trying to delete self
        if (_currentUser.UserId == command.Id)
        {
            throw new CannotDeleteSelfException();
        }

        var user = await _userRepository.GetByIdAsync(command.Id, ct);

        if (user is null)
        {
            throw new SystemUserNotFoundException(command.Id);
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

        // Invalidate permission cache
        await _permissionCache.InvalidateAsync(command.Id, ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.UserDeleted,
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
