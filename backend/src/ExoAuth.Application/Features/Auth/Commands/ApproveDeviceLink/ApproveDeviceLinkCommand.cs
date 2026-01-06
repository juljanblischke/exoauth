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
/// Response for device link approval - device is now trusted, user can retry login on original device.
/// </summary>
public sealed record ApproveDeviceLinkResponse(
    bool Success,
    Guid? DeviceId = null,
    string? RedirectUrl = null,
    string? Message = null
);
