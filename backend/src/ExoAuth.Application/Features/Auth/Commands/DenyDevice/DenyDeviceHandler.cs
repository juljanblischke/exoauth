using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.DenyDevice;

public sealed class DenyDeviceHandler : ICommandHandler<DenyDeviceCommand, DenyDeviceResponse>
{
    private readonly IDeviceApprovalService _deviceApprovalService;
    private readonly IDeviceSessionService _deviceSessionService;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly IEmailService _emailService;
    private readonly ISystemUserRepository _userRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<DenyDeviceHandler> _logger;

    public DenyDeviceHandler(
        IDeviceApprovalService deviceApprovalService,
        IDeviceSessionService deviceSessionService,
        IRevokedSessionService revokedSessionService,
        IEmailService emailService,
        ISystemUserRepository userRepository,
        IAuditService auditService,
        ILogger<DenyDeviceHandler> logger)
    {
        _deviceApprovalService = deviceApprovalService;
        _deviceSessionService = deviceSessionService;
        _revokedSessionService = revokedSessionService;
        _emailService = emailService;
        _userRepository = userRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public async ValueTask<DenyDeviceResponse> Handle(DenyDeviceCommand command, CancellationToken ct)
    {
        // Validate the token
        var request = await _deviceApprovalService.ValidateTokenAsync(command.ApprovalToken, ct);

        if (request is null)
        {
            _logger.LogDebug("Device approval token validation failed for deny request");
            throw new ApprovalTokenInvalidException();
        }

        // Get the user
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            throw new ApprovalTokenInvalidException();
        }

        // Get device session info for the alert email
        var deviceSession = await _deviceSessionService.GetSessionByIdAsync(request.DeviceSessionId, ct);

        // Deny the approval request
        await _deviceApprovalService.DenyAsync(request, ct);

        // Revoke the device session
        if (deviceSession is not null)
        {
            await _deviceSessionService.RevokeSessionAsync(request.DeviceSessionId, ct);
            await _revokedSessionService.RevokeSessionAsync(request.DeviceSessionId, ct);
        }

        // Send security alert email
        await _emailService.SendDeviceDeniedAlertAsync(
            email: user.Email,
            firstName: user.FirstName,
            deviceName: deviceSession?.DeviceName ?? "Unknown Device",
            browser: deviceSession?.Browser,
            operatingSystem: deviceSession?.OperatingSystem,
            location: deviceSession?.LocationDisplay,
            ipAddress: deviceSession?.IpAddress,
            language: user.PreferredLanguage,
            cancellationToken: ct);

        // Audit log
        await _auditService.LogAsync(
            AuditActions.DeviceDenied,
            request.UserId,
            request.UserId,
            "DeviceSession",
            request.DeviceSessionId,
            new
            {
                RiskScore = request.RiskScore,
                DeviceName = deviceSession?.DeviceName,
                IpAddress = deviceSession?.IpAddress
            },
            ct
        );

        _logger.LogWarning(
            "Device denied for user {UserId}, device session {DeviceSessionId}. Security alert sent.",
            request.UserId, request.DeviceSessionId);

        return new DenyDeviceResponse(true, "Device denied. A security alert has been sent to your email.");
    }
}
