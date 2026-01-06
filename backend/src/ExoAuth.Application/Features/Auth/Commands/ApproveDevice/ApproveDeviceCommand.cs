using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.ApproveDevice;

/// <summary>
/// Command to approve a device using the approval token and XXXX-XXXX code.
/// </summary>
public sealed record ApproveDeviceCommand(
    string ApprovalToken,
    string Code
) : ICommand<ApproveDeviceResponse>;

/// <summary>
/// Response for device approval - device is now trusted, user can retry login on original device.
/// </summary>
public sealed record ApproveDeviceResponse(
    bool Success,
    Guid? DeviceId = null,
    string? Message = null
);
