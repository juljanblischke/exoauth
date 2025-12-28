using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Queries.GetSessions;

/// <summary>
/// Query to get all active sessions for the current user.
/// </summary>
public sealed record GetSessionsQuery() : IQuery<List<DeviceSessionDto>>;
