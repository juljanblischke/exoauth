using ExoAuth.Application.Common.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.ApproveDeviceFromSession;

/// <summary>
/// Approves a pending device from an existing trusted session.
/// </summary>
/// <summary>
/// Approves a pending device from an existing trusted session.
/// </summary>
public sealed record ApproveDeviceFromSessionCommand(Guid PendingDeviceId) : ICommand<ApproveDeviceFromSessionResponse>;

public sealed record ApproveDeviceFromSessionResponse(bool Success, string? Error = null);
