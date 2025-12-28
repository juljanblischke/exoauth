using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RevokeSession;

/// <summary>
/// Command to revoke a specific session.
/// </summary>
public sealed record RevokeSessionCommand(Guid SessionId) : ICommand<RevokeSessionResponse>;

public sealed record RevokeSessionResponse(bool Success);
