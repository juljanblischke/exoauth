using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.Register;

public sealed class RegisterHandler : ICommandHandler<RegisterCommand, AuthResponse>
{
    private readonly IAppDbContext _context;
    private readonly ISystemUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IDeviceSessionService _deviceSessionService;
    private readonly IAuditService _auditService;

    public RegisterHandler(
        IAppDbContext context,
        ISystemUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDeviceSessionService deviceSessionService,
        IAuditService auditService)
    {
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _deviceSessionService = deviceSessionService;
        _auditService = auditService;
    }

    public async ValueTask<AuthResponse> Handle(RegisterCommand command, CancellationToken ct)
    {
        // Check if any system users exist
        var anyUsersExist = await _userRepository.AnyExistsAsync(ct);

        if (anyUsersExist)
        {
            // Registration closed for additional system users
            // Later: check for organizationName and create org user
            throw new RegistrationClosedException();
        }

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(command.Email, ct))
        {
            throw new EmailExistsException();
        }

        // Create user
        var passwordHash = _passwordHasher.Hash(command.Password);
        var user = global::ExoAuth.Domain.Entities.SystemUser.Create(
            email: command.Email,
            passwordHash: passwordHash,
            firstName: command.FirstName,
            lastName: command.LastName,
            emailVerified: true // First user is auto-verified
        );

        await _userRepository.AddAsync(user, ct);

        // Get all system permissions and assign to first user
        var allPermissions = await _context.SystemPermissions
            .Select(p => p.Id)
            .ToListAsync(ct);

        await _userRepository.SetUserPermissionsAsync(user.Id, allPermissions, null, ct);

        // Get permission names for token
        var permissionNames = global::ExoAuth.Domain.Constants.SystemPermissions.AllNames.ToList();

        // Create device session for the registration device
        var deviceId = command.DeviceId ?? _deviceSessionService.GenerateDeviceId();
        var (session, _, _) = await _deviceSessionService.CreateOrUpdateSessionAsync(
            user.Id,
            deviceId,
            command.DeviceFingerprint,
            command.UserAgent,
            command.IpAddress,
            ct
        );

        // Generate tokens with session ID
        var accessToken = _tokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            UserType.System,
            permissionNames,
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
            AuditActions.UserRegistered,
            user.Id,
            null, // targetUserId
            "SystemUser",
            user.Id,
            new { user.Email, IsFirstUser = true },
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
                Permissions: permissionNames
            ),
            AccessToken: accessToken,
            RefreshToken: refreshTokenString
        );
    }
}
