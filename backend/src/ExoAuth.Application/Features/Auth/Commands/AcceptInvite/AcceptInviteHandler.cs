using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.AcceptInvite;

public sealed class AcceptInviteHandler : ICommandHandler<AcceptInviteCommand, AuthResponse>
{
    private readonly IAppDbContext _context;
    private readonly ISystemUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly ISystemInviteService _inviteService;

    public AcceptInviteHandler(
        IAppDbContext context,
        ISystemUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IAuditService auditService,
        ISystemInviteService inviteService)
    {
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _auditService = auditService;
        _inviteService = inviteService;
    }

    public async ValueTask<AuthResponse> Handle(AcceptInviteCommand command, CancellationToken ct)
    {
        // Find invite by token (hash-based lookup)
        var invite = await _inviteService.ValidateTokenAsync(command.Token, ct);

        if (invite is null)
        {
            throw new InviteInvalidException();
        }

        if (invite.IsAccepted)
        {
            throw new InviteInvalidException();
        }

        if (invite.IsExpired)
        {
            throw new InviteExpiredException();
        }

        // Check if email already exists (race condition protection)
        if (await _userRepository.EmailExistsAsync(invite.Email, ct))
        {
            throw new EmailExistsException();
        }

        // Create user
        var passwordHash = _passwordHasher.Hash(command.Password);
        var user = global::ExoAuth.Domain.Entities.SystemUser.Create(
            email: invite.Email,
            passwordHash: passwordHash,
            firstName: invite.FirstName,
            lastName: invite.LastName,
            emailVerified: true
        );

        await _userRepository.AddAsync(user, ct);

        // Set permissions from invite
        var permissionIds = invite.GetPermissionIds();
        await _userRepository.SetUserPermissionsAsync(user.Id, permissionIds, invite.InvitedBy, ct);

        // Mark invite as accepted
        invite.Accept();

        // Get permission names
        var permissions = await _userRepository.GetUserPermissionNamesAsync(user.Id, ct);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            UserType.System,
            permissions
        );

        var refreshTokenString = _tokenService.GenerateRefreshToken();
        var refreshToken = global::ExoAuth.Domain.Entities.RefreshToken.Create(
            userId: user.Id,
            userType: UserType.System,
            token: refreshTokenString,
            expirationDays: (int)_tokenService.RefreshTokenExpiration.TotalDays
        );

        await _context.RefreshTokens.AddAsync(refreshToken, ct);
        await _context.SaveChangesAsync(ct);

        // Record login
        user.RecordLogin();
        await _userRepository.UpdateAsync(user, ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.UserInviteAccepted,
            user.Id,
            null, // targetUserId
            "SystemUser",
            user.Id,
            new { InviteId = invite.Id, InvitedBy = invite.InvitedBy },
            ct
        );

        return new AuthResponse(
            User: new UserDto(
                Id: user.Id,
                Email: user.Email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                FullName: user.FullName,
                IsActive: user.IsActive,
                EmailVerified: user.EmailVerified,
                LastLoginAt: user.LastLoginAt,
                CreatedAt: user.CreatedAt,
                Permissions: permissions
            ),
            AccessToken: accessToken,
            RefreshToken: refreshTokenString
        );
    }
}
