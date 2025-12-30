using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Queries.GetSystemUser;

public sealed class GetSystemUserHandler : IQueryHandler<GetSystemUserQuery, SystemUserDetailDto>
{
    private readonly ISystemUserRepository _userRepository;

    public GetSystemUserHandler(ISystemUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async ValueTask<SystemUserDetailDto> Handle(GetSystemUserQuery query, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdWithPermissionsAsync(query.Id, ct);

        if (user is null)
        {
            throw new SystemUserNotFoundException(query.Id);
        }

        var permissions = user.Permissions.Select(p => new PermissionDto(
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
