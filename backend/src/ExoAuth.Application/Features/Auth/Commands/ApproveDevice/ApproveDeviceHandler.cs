using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.ApproveDevice;

public sealed class ApproveDeviceHandler : ICommandHandler<ApproveDeviceCommand, ApproveDeviceResponse>
{
    private readonly IDeviceApprovalService _deviceApprovalService;
    private readonly IDeviceSessionService _deviceSessionService;
    private readonly ITrustedDeviceService _trustedDeviceService;
    private readonly IGeoIpService _geoIpService;
    private readonly IDeviceDetectionService _deviceDetectionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ApproveDeviceHandler> _logger;

    public ApproveDeviceHandler(
        IDeviceApprovalService deviceApprovalService,
        IDeviceSessionService deviceSessionService,
        ITrustedDeviceService trustedDeviceService,
        IGeoIpService geoIpService,
        IDeviceDetectionService deviceDetectionService,
        IAuditService auditService,
        ILogger<ApproveDeviceHandler> logger)
    {
        _deviceApprovalService = deviceApprovalService;
        _deviceSessionService = deviceSessionService;
        _trustedDeviceService = trustedDeviceService;
        _geoIpService = geoIpService;
        _deviceDetectionService = deviceDetectionService;
        _auditService = auditService;
        _logger = logger;
    }

    public async ValueTask<ApproveDeviceResponse> Handle(ApproveDeviceCommand command, CancellationToken ct)
    {
        // Validate the code against the approval token
        var result = await _deviceApprovalService.ValidateCodeAsync(command.ApprovalToken, command.Code, ct);

        if (!result.IsValid)
        {
            _logger.LogDebug("Device approval code validation failed: {ErrorCode}", result.ErrorCode);

            // Throw appropriate exception based on error code
            throw result.ErrorCode switch
            {
                ErrorCodes.ApprovalTokenInvalid => new ApprovalTokenInvalidException(),
                ErrorCodes.ApprovalMaxAttempts => new ApprovalMaxAttemptsException(),
                ErrorCodes.ApprovalCodeInvalid => new ApprovalCodeInvalidException(result.RemainingAttempts),
                _ => new ApprovalTokenInvalidException()
            };
        }

        var request = result.Request!;

        // Approve the device
        await _deviceApprovalService.ApproveAsync(request, "email_code", ct);

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
            AuditActions.DeviceApprovedViaCode,
            request.UserId,
            request.UserId,
            "DeviceSession",
            request.DeviceSessionId,
            new { RiskScore = request.RiskScore },
            ct
        );

        _logger.LogInformation(
            "Device approved via code for user {UserId}, device session {DeviceSessionId}",
            request.UserId, request.DeviceSessionId);

        return new ApproveDeviceResponse(true, "Device approved successfully. You can now login.");
    }
}
