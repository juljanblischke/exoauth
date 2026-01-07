using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.ApproveDevice;

public sealed class ApproveDeviceHandler : ICommandHandler<ApproveDeviceCommand, ApproveDeviceResponse>
{
    private readonly IDeviceService _deviceService;
    private readonly IAuditService _auditService;
    private readonly ICaptchaService _captchaService;
    private readonly ILogger<ApproveDeviceHandler> _logger;

    public ApproveDeviceHandler(
        IDeviceService deviceService,
        IAuditService auditService,
        ICaptchaService captchaService,
        ILogger<ApproveDeviceHandler> logger)
    {
        _deviceService = deviceService;
        _auditService = auditService;
        _captchaService = captchaService;
        _logger = logger;
    }

    public async ValueTask<ApproveDeviceResponse> Handle(ApproveDeviceCommand command, CancellationToken ct)
    {
        // First, validate the token to get the device (without incrementing attempts)
        var device = await _deviceService.ValidateApprovalTokenAsync(command.ApprovalToken, ct);
        if (device is null)
        {
            throw new ApprovalTokenInvalidException();
        }

        // Check if CAPTCHA is required based on device's previous failed attempts
        var captchaRequired = await _captchaService.IsRequiredForDeviceApprovalAsync(device.Id, ct);
        await _captchaService.ValidateConditionalAsync(
            command.CaptchaToken,
            captchaRequired,
            "device_approval",
            command.IpAddress,
            ct);

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

        // Use the device from the result (same as from token validation)
        device = result.Device!;

        // Mark device as trusted
        await _deviceService.MarkDeviceTrustedAsync(device, ct);

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
            "Device approved via code for user {UserId}, device {DeviceId}. User can now retry login.",
            device.UserId, device.Id);

        return new ApproveDeviceResponse(
            Success: true,
            DeviceId: device.Id,
            Message: "Device approved. Please retry login on your device."
        );
    }
}
