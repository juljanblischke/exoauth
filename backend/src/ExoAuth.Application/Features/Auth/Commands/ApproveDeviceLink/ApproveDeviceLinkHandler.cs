using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.ApproveDeviceLink;

public sealed class ApproveDeviceLinkHandler : ICommandHandler<ApproveDeviceLinkCommand, ApproveDeviceLinkResponse>
{
    private readonly IDeviceService _deviceService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ApproveDeviceLinkHandler> _logger;
    private readonly string _baseUrl;

    public ApproveDeviceLinkHandler(
        IDeviceService deviceService,
        IAuditService auditService,
        IConfiguration configuration,
        ILogger<ApproveDeviceLinkHandler> logger)
    {
        _deviceService = deviceService;
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

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.DeviceApprovedViaLink,
            device.UserId,
            device.UserId,
            "Device",
            device.Id,
            new { device.RiskScore },
            ct
        );

        _logger.LogInformation(
            "Device approved via link for user {UserId}, device {DeviceId}. User can now retry login.",
            device.UserId, device.Id);

        // Return redirect URL (user can retry login on original device)
        var redirectUrl = $"{_baseUrl}/login?device_approved=true";

        return new ApproveDeviceLinkResponse(
            Success: true,
            DeviceId: device.Id,
            RedirectUrl: redirectUrl,
            Message: "Device approved. Please retry login on your device."
        );
    }
}
