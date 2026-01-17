using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.DenyDevice;

/// <summary>
/// Command to deny a device approval request.
/// This will revoke the device session and send a security alert to the user.
/// </summary>
public sealed record DenyDeviceCommand(
    string ApprovalToken
) : ICommand<DenyDeviceResponse>;

/// <summary>
/// Response for device denial.
/// </summary>
public sealed record DenyDeviceResponse(
    bool Success,
    string Message
);
