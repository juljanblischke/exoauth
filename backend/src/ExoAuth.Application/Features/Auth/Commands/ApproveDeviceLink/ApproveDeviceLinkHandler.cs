using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.ApproveDeviceLink;

public sealed class ApproveDeviceLinkHandler : ICommandHandler<ApproveDeviceLinkCommand, ApproveDeviceLinkResponse>
{
    private readonly IAppDbContext _context;
    private readonly IDeviceService _deviceService;
    private readonly ISystemUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPermissionCacheService _permissionCache;
    private readonly IAuditService _auditService;
    private readonly ILogger<ApproveDeviceLinkHandler> _logger;
    private readonly string _baseUrl;

    public ApproveDeviceLinkHandler(
        IAppDbContext context,
        IDeviceService deviceService,
        ISystemUserRepository userRepository,
        ITokenService tokenService,
        IPermissionCacheService permissionCache,
        IAuditService auditService,
        IConfiguration configuration,
        ILogger<ApproveDeviceLinkHandler> logger)
    {
        _context = context;
        _deviceService = deviceService;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _permissionCache = permissionCache;
        _auditService = auditService;
        _logger = logger;
        _baseUrl = configuration.GetValue<string>("SystemInvite:BaseUrl") ?? "http://localhost:5173";
    }

    public async ValueTask<ApproveDeviceLinkResponse> Handle(ApproveDeviceLinkCommand command, CancellationToken ct)
    {
        // Validate the token
        var device = await _deviceService.ValidateApprovalTokenAsync(command.Token, ct);

        if (device is null)
        {
            _logger.LogDebug("Device approval token validation failed");
            throw new ApprovalTokenInvalidException();
        }

        // Mark device as trusted
        await _deviceService.MarkDeviceTrustedAsync(device, ct);

        // Get user for token generation
        var user = await _userRepository.GetByIdAsync(device.UserId, ct);
        if (user is null)
        {
            throw new ApprovalTokenInvalidException();
        }

        // Get permissions
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
            permissions,
            device.Id  // Device ID as session ID
        );

        var refreshTokenString = _tokenService.GenerateRefreshToken();
        var refreshToken = Domain.Entities.RefreshToken.Create(
            userId: user.Id,
            userType: UserType.System,
            token: refreshTokenString,
            expirationDays: 30
        );

        // Link refresh token to device
        refreshToken.LinkToDevice(device.Id);

        await _context.RefreshTokens.AddAsync(refreshToken, ct);
        await _context.SaveChangesAsync(ct);

        // Record login
        user.RecordLogin();
        await _userRepository.UpdateAsync(user, ct);

        // Audit log
        await _auditService.LogAsync(
            AuditActions.DeviceApprovedViaLink,
            device.UserId,
            device.UserId,
            "Device",
            device.Id,
            new { device.RiskScore },
            ct
        );

        _logger.LogInformation(
            "Device approved via link for user {UserId}, device {DeviceId}. Tokens issued.",
            device.UserId, device.Id);

        // Return redirect URL with tokens (frontend will handle storage)
        var redirectUrl = $"{_baseUrl}/login?device_approved=true";

        return new ApproveDeviceLinkResponse(
            Success: true,
            AccessToken: accessToken,
            RefreshToken: refreshTokenString,
            DeviceId: device.Id,
            RedirectUrl: redirectUrl,
            Message: "Device approved successfully."
        );
    }
}
