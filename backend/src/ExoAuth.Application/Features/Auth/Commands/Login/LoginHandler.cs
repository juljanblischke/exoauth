using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Enums;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.Login;

public sealed class LoginHandler : ICommandHandler<LoginCommand, AuthResponse>
{
    private readonly IAppDbContext _context;
    private readonly ISystemUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IBruteForceProtectionService _bruteForceService;
    private readonly IPermissionCacheService _permissionCache;
    private readonly IAuditService _auditService;

    public LoginHandler(
        IAppDbContext context,
        ISystemUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IBruteForceProtectionService bruteForceService,
        IPermissionCacheService permissionCache,
        IAuditService auditService)
    {
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _bruteForceService = bruteForceService;
        _permissionCache = permissionCache;
        _auditService = auditService;
    }

    public async ValueTask<AuthResponse> Handle(LoginCommand command, CancellationToken ct)
    {
        var email = command.Email.ToLowerInvariant();

        // Check if blocked due to too many attempts
        if (await _bruteForceService.IsBlockedAsync(email, ct))
        {
            await _auditService.LogWithContextAsync(
                AuditActions.LoginBlocked,
                null,
                null,
                null,
                new { Email = email, Reason = "Too many failed attempts" },
                ct
            );
            throw new TooManyAttemptsException();
        }

        // Find user
        var user = await _userRepository.GetByEmailAsync(email, ct);

        if (user is null)
        {
            await RecordFailedAttempt(email, "User not found", ct);
            throw new InvalidCredentialsException();
        }

        // Verify password
        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            await RecordFailedAttempt(email, "Invalid password", ct);
            throw new InvalidCredentialsException();
        }

        // Check if active
        if (!user.IsActive)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.UserLoginFailed,
                user.Id,
                "SystemUser",
                user.Id,
                new { Reason = "User inactive" },
                ct
            );
            throw new UserInactiveException();
        }

        // Reset brute force counter on successful login
        await _bruteForceService.ResetAsync(email, ct);

        // Get permissions (with caching)
        var permissions = await _permissionCache.GetOrSetPermissionsAsync(
            user.Id,
            () => _userRepository.GetUserPermissionNamesAsync(user.Id, ct),
            ct
        );

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
            AuditActions.UserLogin,
            user.Id,
            "SystemUser",
            user.Id,
            null,
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

    private async Task RecordFailedAttempt(string email, string reason, CancellationToken ct)
    {
        var (attempts, isBlocked) = await _bruteForceService.RecordFailedAttemptAsync(email, ct);

        await _auditService.LogWithContextAsync(
            AuditActions.UserLoginFailed,
            null,
            null,
            null,
            new { Email = email, Reason = reason, Attempts = attempts, IsBlocked = isBlocked },
            ct
        );

        if (isBlocked)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.LoginBlocked,
                null,
                null,
                null,
                new { Email = email, Attempts = attempts },
                ct
            );
        }
    }
}
