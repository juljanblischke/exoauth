using ExoAuth.Application.Features.SystemInvites.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemInvites.Queries.ValidateInvite;

/// <summary>
/// Query to validate an invite token (public endpoint).
/// </summary>
public sealed record ValidateInviteQuery(string Token) : IQuery<InviteValidationDto>;
