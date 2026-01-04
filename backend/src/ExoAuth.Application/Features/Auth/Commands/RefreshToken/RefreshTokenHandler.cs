using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenHandler : ICommandHandler<RefreshTokenCommand, TokenResponse>
{
    private readonly IAppDbContext _context;
    private readonly ISystemUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ITokenBlacklistService _tokenBlacklist;
    private readonly IPermissionCacheService _permissionCache;
    private readonly IDeviceService _deviceService;
    private readonly IForceReauthService _forceReauthService;
    private readonly IAuditService _auditService;

    public RefreshTokenHandler(
        IAppDbContext context,
        ISystemUserRepository userRepository,
        ITokenService tokenService,
        ITokenBlacklistService tokenBlacklist,
        IPermissionCacheService permissionCache,
        IDeviceService deviceService,
        IForceReauthService forceReauthService,
        IAuditService auditService)
    {
        _context = context;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _tokenBlacklist = tokenBlacklist;
        _permissionCache = permissionCache;
        _deviceService = deviceService;
        _forceReauthService = forceReauthService;
        _auditService = auditService;
    }

    public async ValueTask<TokenResponse> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        // Find all non-revoked refresh tokens and check the hash
        var tokens = await _context.RefreshTokens
            .Where(t => !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        var storedToken = tokens.FirstOrDefault(t => t.ValidateToken(command.RefreshToken));

        if (storedToken is null)
        {
            throw new InvalidRefreshTokenException();
        }

        // Check if blacklisted (extra safety)
        if (await _tokenBlacklist.IsBlacklistedAsync(storedToken.Id, ct))
        {
            throw new InvalidRefreshTokenException();
        }

        // Get user based on token type
        if (storedToken.UserType != UserType.System)
        {
            // TODO: Handle organization and project users in future tasks
            throw new InvalidRefreshTokenException();
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, ct);

        if (user is null || !user.IsActive || user.IsLocked)
        {
            throw new InvalidRefreshTokenException();
        }

        // Check if user has force re-auth flag - must re-login, can't just refresh
        if (await _forceReauthService.HasFlagAsync(user.Id, ct))
        {
            throw new ForceReauthException();
        }

        // Revoke old token
        storedToken.Revoke();
        await _tokenBlacklist.BlacklistAsync(storedToken.Id, storedToken.ExpiresAt, ct);

        // Get the device ID from the old token
        var deviceId = storedToken.DeviceId;

        // Record activity on the device if available
        if (deviceId.HasValue)
        {
            await _deviceService.RecordUsageAsync(deviceId.Value, command.IpAddress, null, null, ct);
        }

        // Get permissions
        var permissions = await _permissionCache.GetOrSetPermissionsAsync(
            user.Id,
            () => _userRepository.GetUserPermissionNamesAsync(user.Id, ct),
            ct
        );

        // Generate new tokens with device ID as session ID
        var accessToken = _tokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            UserType.System,
            permissions,
            deviceId
        );

        // Preserve RememberMe setting from old token
        var expirationDays = storedToken.RememberMe
            ? 30
            : (int)_tokenService.RefreshTokenExpiration.TotalDays;

        var newRefreshTokenString = _tokenService.GenerateRefreshToken();
        var newRefreshToken = Domain.Entities.RefreshToken.Create(
            userId: user.Id,
            userType: UserType.System,
            token: newRefreshTokenString,
            expirationDays: expirationDays
        );

        // Link new refresh token to the same device
        if (deviceId.HasValue)
        {
            newRefreshToken.LinkToDevice(deviceId.Value);
        }

        await _context.RefreshTokens.AddAsync(newRefreshToken, ct);
        await _context.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.TokenRefreshed,
            user.Id,
            null, // targetUserId
            "SystemUser",
            user.Id,
            new { DeviceId = deviceId },
            ct
        );

        return new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: newRefreshTokenString,
            SessionId: deviceId
        );
    }
}
