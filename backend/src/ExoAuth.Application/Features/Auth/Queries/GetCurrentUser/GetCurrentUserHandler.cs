using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserHandler : IQueryHandler<GetCurrentUserQuery, UserDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ISystemUserRepository _userRepository;
    private readonly IPermissionCacheService _permissionCache;

    public GetCurrentUserHandler(
        ICurrentUserService currentUser,
        ISystemUserRepository userRepository,
        IPermissionCacheService permissionCache)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _permissionCache = permissionCache;
    }

    public async ValueTask<UserDto> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
        {
            throw new AuthException("AUTH_UNAUTHORIZED", "User is not authenticated", 401);
        }

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId.Value, ct);

        if (user is null)
        {
            throw new AuthException("AUTH_UNAUTHORIZED", "User not found", 401);
        }

        if (!user.IsActive)
        {
            throw new UserInactiveException();
        }

        if (user.IsLocked)
        {
            throw new AccountLockedException(user.LockedUntil);
        }

        var permissions = await _permissionCache.GetOrSetPermissionsAsync(
            user.Id,
            () => _userRepository.GetUserPermissionNamesAsync(user.Id, ct),
            ct
        );

        return new UserDto(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            FullName: user.FullName,
            IsActive: user.IsActive,
            EmailVerified: user.EmailVerified,
            MfaEnabled: user.MfaEnabled,
            PreferredLanguage: user.PreferredLanguage,
            LastLoginAt: user.LastLoginAt,
            CreatedAt: user.CreatedAt,
            Permissions: permissions
        );
    }
}
