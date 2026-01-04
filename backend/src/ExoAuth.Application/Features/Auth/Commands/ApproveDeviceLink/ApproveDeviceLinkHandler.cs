using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.ApproveDeviceLink;

public sealed class ApproveDeviceLinkHandler : ICommandHandler<ApproveDeviceLinkCommand, ApproveDeviceLinkResponse>
{
    private readonly IDeviceApprovalService _deviceApprovalService;
    private readonly IDeviceSessionService _deviceSessionService;
    private readonly ITrustedDeviceService _trustedDeviceService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ApproveDeviceLinkHandler> _logger;
    private readonly string _baseUrl;

    public ApproveDeviceLinkHandler(
        IDeviceApprovalService deviceApprovalService,
        IDeviceSessionService deviceSessionService,
        ITrustedDeviceService trustedDeviceService,
        IAuditService auditService,
        IConfiguration configuration,
        ILogger<ApproveDeviceLinkHandler> logger)
    {
        _deviceApprovalService = deviceApprovalService;
        _deviceSessionService = deviceSessionService;
        _trustedDeviceService = trustedDeviceService;
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

        // Get the session to create a trusted device entry
        var session = await _deviceSessionService.GetSessionByIdAsync(request.DeviceSessionId, ct);
        if (session is not null)
        {
            // Get device info and location from the session
            var deviceInfo = new DeviceInfo(
                session.Browser,
                session.BrowserVersion,
                session.OperatingSystem,
                session.OsVersion,
                session.DeviceType);

            var geoLocation = new GeoLocation(
                session.IpAddress,
                session.Country,
                session.CountryCode,
                session.City,
                session.Latitude,
                session.Longitude);

            // Add to trusted devices
            var trustedDevice = await _trustedDeviceService.AddAsync(
                request.UserId,
                session.DeviceId,
                deviceInfo,
                geoLocation,
                session.DeviceFingerprint,
                ct);

            // Link session to trusted device
            await _deviceSessionService.LinkToTrustedDeviceAsync(request.DeviceSessionId, trustedDevice.Id, ct);
        }

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
