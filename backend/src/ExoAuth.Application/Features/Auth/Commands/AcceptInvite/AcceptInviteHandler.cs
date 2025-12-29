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
    private readonly IDeviceSessionService _deviceSessionService;
    private readonly IAuditService _auditService;
    private readonly ISystemInviteService _inviteService;

    public AcceptInviteHandler(
        IAppDbContext context,
        ISystemUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDeviceSessionService deviceSessionService,
        IAuditService auditService,
        ISystemInviteService inviteService)
    {
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _deviceSessionService = deviceSessionService;
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

        if (invite.IsRevoked)
        {
            throw new AuthException("AUTH_INVITE_REVOKED", "This invitation has been revoked", 400);
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

        // Set preferred language
        user.SetPreferredLanguage(command.Language);

        // Ensure unique ID (handles extremely rare GUID collision)
        const int maxRetries = 3;
        for (var i = 0; i < maxRetries; i++)
        {
            if (!await _context.SystemUsers.AnyAsync(u => u.Id == user.Id, ct))
                break;
            user.RegenerateId();
            if (i == maxRetries - 1)
                throw new InvalidOperationException("Failed to generate unique user ID after multiple attempts");
        }

        await _userRepository.AddAsync(user, ct);

        // Set permissions from invite
        var permissionIds = invite.GetPermissionIds();
        await _userRepository.SetUserPermissionsAsync(user.Id, permissionIds, invite.InvitedBy, ct);

        // Mark invite as accepted
        invite.Accept();

        // Get permission names
        var permissions = await _userRepository.GetUserPermissionNamesAsync(user.Id, ct);

        // Create device session for the accept-invite device
        var deviceId = command.DeviceId ?? _deviceSessionService.GenerateDeviceId();
        var (session, _, _) = await _deviceSessionService.CreateOrUpdateSessionAsync(
            user.Id,
            deviceId,
            command.UserAgent,
            command.IpAddress,
            command.DeviceFingerprint,
            ct
        );

        // Generate tokens with session ID
        var accessToken = _tokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            UserType.System,
            permissions,
            session.Id
        );

        var refreshTokenString = _tokenService.GenerateRefreshToken();
        var refreshToken = global::ExoAuth.Domain.Entities.RefreshToken.Create(
            userId: user.Id,
            userType: UserType.System,
            token: refreshTokenString,
            expirationDays: (int)_tokenService.RefreshTokenExpiration.TotalDays
        );

        // Link refresh token to device session
        refreshToken.LinkToSession(session.Id);

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
            new { InviteId = invite.Id, InvitedBy = invite.InvitedBy, SessionId = session.Id },
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
                MfaEnabled: user.MfaEnabled,
                PreferredLanguage: user.PreferredLanguage,
                LastLoginAt: user.LastLoginAt,
                CreatedAt: user.CreatedAt,
                Permissions: permissions
            ),
            AccessToken: accessToken,
            RefreshToken: refreshTokenString,
            SessionId: session.Id,
            DeviceId: deviceId
        );
    }
}
