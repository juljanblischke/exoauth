using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.ResendDeviceApproval;

/// <summary>
/// Command to resend a device approval email.
/// Requires the approval token from the original email to identify the pending device.
/// </summary>
public sealed record ResendDeviceApprovalCommand(
    string ApprovalToken,
    string? CaptchaToken = null,
    string? IpAddress = null
) : ICommand<ResendDeviceApprovalResponse>;

/// <summary>
/// Response for resend device approval request.
/// </summary>
public sealed record ResendDeviceApprovalResponse(
    bool Success,
    string Message,
    string? NewApprovalToken = null
);
