using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.ApproveDevice;

public sealed class ApproveDeviceHandler : ICommandHandler<ApproveDeviceCommand, ApproveDeviceResponse>
{
    private readonly IAppDbContext _context;
    private readonly IDeviceService _deviceService;
    private readonly ISystemUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPermissionCacheService _permissionCache;
    private readonly IAuditService _auditService;
    private readonly ILogger<ApproveDeviceHandler> _logger;

    public ApproveDeviceHandler(
        IAppDbContext context,
        IDeviceService deviceService,
        ISystemUserRepository userRepository,
        ITokenService tokenService,
        IPermissionCacheService permissionCache,
        IAuditService auditService,
        ILogger<ApproveDeviceHandler> logger)
    {
        _context = context;
        _deviceService = deviceService;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _permissionCache = permissionCache;
        _auditService = auditService;
        _logger = logger;
    }

    public async ValueTask<ApproveDeviceResponse> Handle(ApproveDeviceCommand command, CancellationToken ct)
    {
        // Validate the code against the approval token
        var result = await _deviceService.ValidateApprovalCodeAsync(command.ApprovalToken, command.Code, ct);

        if (!result.IsValid)
        {
            _logger.LogDebug("Device approval code validation failed: {Error}", result.Error);

            throw result.Error switch
            {
                "APPROVAL_TOKEN_INVALID" => new ApprovalTokenInvalidException(),
                "APPROVAL_TOKEN_EXPIRED" => new ApprovalTokenInvalidException(),
                "APPROVAL_MAX_ATTEMPTS" => new ApprovalMaxAttemptsException(),
                "APPROVAL_CODE_INVALID" => new ApprovalCodeInvalidException(3 - result.Attempts),
                _ => new ApprovalTokenInvalidException()
            };
        }

        var device = result.Device!;

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
            AuditActions.DeviceApprovedViaCode,
            device.UserId,
            device.UserId,
            "Device",
            device.Id,
            new { device.RiskScore },
            ct
        );

        _logger.LogInformation(
            "Device approved via code for user {UserId}, device {DeviceId}. Tokens issued.",
            device.UserId, device.Id);

        return new ApproveDeviceResponse(
            Success: true,
            AccessToken: accessToken,
            RefreshToken: refreshTokenString,
            DeviceId: device.Id,
            Message: "Device approved successfully."
        );
    }
}
