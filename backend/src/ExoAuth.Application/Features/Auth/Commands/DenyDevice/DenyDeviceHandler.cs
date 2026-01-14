using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.DenyDevice;

public sealed class DenyDeviceHandler : ICommandHandler<DenyDeviceCommand, DenyDeviceResponse>
{
    private readonly IDeviceService _deviceService;
    private readonly IRevokedSessionService _revokedSessionService;
    private readonly IEmailService _emailService;
    private readonly ISystemUserRepository _userRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<DenyDeviceHandler> _logger;

    public DenyDeviceHandler(
        IDeviceService deviceService,
        IRevokedSessionService revokedSessionService,
        IEmailService emailService,
        ISystemUserRepository userRepository,
        IAuditService auditService,
        ILogger<DenyDeviceHandler> logger)
    {
        _deviceService = deviceService;
        _revokedSessionService = revokedSessionService;
        _emailService = emailService;
        _userRepository = userRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public async ValueTask<DenyDeviceResponse> Handle(DenyDeviceCommand command, CancellationToken ct)
    {
        // Validate the token and get the device
        var device = await _deviceService.ValidateApprovalTokenAsync(command.ApprovalToken, ct);

        if (device is null)
        {
            _logger.LogDebug("Device approval token validation failed for deny request");
            throw new ApprovalTokenInvalidException();
        }

        // Get the user
        var user = await _userRepository.GetByIdAsync(device.UserId, ct);
        if (user is null)
        {
            throw new ApprovalTokenInvalidException();
        }

        // Revoke the device
        await _deviceService.RevokeAsync(device.Id, ct);
        await _revokedSessionService.RevokeSessionAsync(device.Id, ct);

        // Send security alert email
        await _emailService.SendDeviceDeniedAlertAsync(
            email: user.Email,
            firstName: user.FirstName,
            deviceName: device.DisplayName,
            browser: device.Browser,
            operatingSystem: device.OperatingSystem,
            location: device.LocationDisplay,
            ipAddress: device.IpAddress,
            userId: user.Id,
            language: user.PreferredLanguage,
            cancellationToken: ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.DeviceDenied,
            device.UserId,
            device.UserId,
            "Device",
            device.Id,
            new
            {
                device.RiskScore,
                DeviceName = device.DisplayName,
                device.IpAddress
            },
            ct
        );

        _logger.LogWarning(
            "Device denied for user {UserId}, device {DeviceId}. Security alert sent.",
            device.UserId, device.Id);

        return new DenyDeviceResponse(true, "Device denied. A security alert has been sent to your email.");
    }
}
