using ExoAuth.Application.Features.SystemInvites.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemInvites.Queries.GetSystemInvite;

/// <summary>
/// Query to get a single system invite by ID.
/// </summary>
public sealed record GetSystemInviteQuery(Guid Id) : IQuery<SystemInviteDetailDto?>;
