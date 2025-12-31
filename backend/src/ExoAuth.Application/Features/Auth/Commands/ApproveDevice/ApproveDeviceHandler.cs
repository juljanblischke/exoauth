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
    private readonly IAuditService _auditService;
    private readonly ILogger<ApproveDeviceHandler> _logger;

    public ApproveDeviceHandler(
        IDeviceApprovalService deviceApprovalService,
        IDeviceSessionService deviceSessionService,
        IAuditService auditService,
        ILogger<ApproveDeviceHandler> logger)
    {
        _deviceApprovalService = deviceApprovalService;
        _deviceSessionService = deviceSessionService;
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

        // Trust the device session
        await _deviceSessionService.SetTrustStatusAsync(request.DeviceSessionId, true, ct);

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
