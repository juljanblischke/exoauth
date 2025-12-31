using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.ApproveDeviceLink;

/// <summary>
/// Command to approve a device using the email link token.
/// This is triggered when user clicks the approval link in the email.
/// </summary>
public sealed record ApproveDeviceLinkCommand(
    string Token
) : ICommand<ApproveDeviceLinkResponse>;

/// <summary>
/// Response for device link approval.
/// </summary>
public sealed record ApproveDeviceLinkResponse(
    bool Success,
    string Message,
    string? RedirectUrl = null
);
