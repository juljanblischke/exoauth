using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.UpdateSession;

/// <summary>
/// Command to update a session (rename or set trust status).
/// </summary>
public sealed record UpdateSessionCommand(
    Guid SessionId,
    string? Name = null
) : ICommand<DeviceSessionDto>;
