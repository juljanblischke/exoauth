using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.ApproveDeviceLink;

public sealed class ApproveDeviceLinkHandler : ICommandHandler<ApproveDeviceLinkCommand, ApproveDeviceLinkResponse>
{
    private readonly IDeviceApprovalService _deviceApprovalService;
    private readonly IDeviceSessionService _deviceSessionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ApproveDeviceLinkHandler> _logger;
    private readonly string _baseUrl;

    public ApproveDeviceLinkHandler(
        IDeviceApprovalService deviceApprovalService,
        IDeviceSessionService deviceSessionService,
        IAuditService auditService,
        IConfiguration configuration,
        ILogger<ApproveDeviceLinkHandler> logger)
    {
        _deviceApprovalService = deviceApprovalService;
        _deviceSessionService = deviceSessionService;
        _auditService = auditService;
        _logger = logger;
        _baseUrl = configuration.GetValue<string>("SystemInvite:BaseUrl") ?? "http://localhost:5173";
    }

    public async ValueTask<ApproveDeviceLinkResponse> Handle(ApproveDeviceLinkCommand command, CancellationToken ct)
    {
        // Validate the token
        var request = await _deviceApprovalService.ValidateTokenAsync(command.Token, ct);

        if (request is null)
        {
            _logger.LogDebug("Device approval token validation failed");
            throw new ApprovalTokenInvalidException();
        }

        // Approve the device
        await _deviceApprovalService.ApproveAsync(request, "email_link", ct);

        // Trust the device session
        await _deviceSessionService.SetTrustStatusAsync(request.DeviceSessionId, true, ct);

        // Audit log
        await _auditService.LogAsync(
            AuditActions.DeviceApprovedViaLink,
            request.UserId,
            request.UserId,
            "DeviceSession",
            request.DeviceSessionId,
            new { RiskScore = request.RiskScore },
            ct
        );

        _logger.LogInformation(
            "Device approved via link for user {UserId}, device session {DeviceSessionId}",
            request.UserId, request.DeviceSessionId);

        // Return redirect URL to login page
        var redirectUrl = $"{_baseUrl}/login?device_approved=true";

        return new ApproveDeviceLinkResponse(
            true,
            "Device approved successfully. You can now login.",
            redirectUrl);
    }
}
