using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.ActivateSystemUser;

public sealed class ActivateSystemUserHandler : ICommandHandler<ActivateSystemUserCommand, bool>
{
    private readonly ISystemUserRepository _userRepository;
    private readonly IPermissionCacheService _permissionCache;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public ActivateSystemUserHandler(
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

    public async ValueTask<bool> Handle(ActivateSystemUserCommand command, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(command.Id, ct);

        if (user is null)
        {
            throw new SystemUserNotFoundException(command.Id);
        }

        if (user.IsAnonymized)
        {
            throw new UserAnonymizedException(command.Id);
        }

        if (user.IsActive)
        {
            throw new UserAlreadyActivatedException(command.Id);
        }

        // Activate user
        user.Activate();
        await _userRepository.UpdateAsync(user, ct);

        // Invalidate permission cache (in case permissions were cached while inactive)
        await _permissionCache.InvalidateAsync(command.Id, ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.UserActivated,
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
