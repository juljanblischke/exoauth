using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Commands.UpdateSystemUser;

public sealed class UpdateSystemUserHandler : ICommandHandler<UpdateSystemUserCommand, SystemUserDto>
{
    private readonly ISystemUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public UpdateSystemUserHandler(
        ISystemUserRepository userRepository,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async ValueTask<SystemUserDto> Handle(UpdateSystemUserCommand command, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(command.Id, ct);

        if (user is null)
        {
            throw new SystemUserNotFoundException(command.Id);
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

        // Update user
        user.Update(
            firstName: command.FirstName,
            lastName: command.LastName,
            isActive: command.IsActive
        );

        await _userRepository.UpdateAsync(user, ct);

        // Audit log
        if (changes.Count > 0)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.UserUpdated,
                _currentUser.UserId,
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
            LastLoginAt: user.LastLoginAt,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt
        );
    }
}
