using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RevokeAllSessions;

/// <summary>
/// Command to revoke all sessions except the current one.
/// </summary>
public sealed record RevokeAllSessionsCommand() : ICommand<RevokeAllSessionsResponse>;

public sealed record RevokeAllSessionsResponse(int RevokedCount);
