using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Queries.GetCurrentUser;

/// <summary>
/// Query to get the current authenticated user's information.
/// </summary>
public sealed record GetCurrentUserQuery : IQuery<UserDto>;
