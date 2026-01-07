using ExoAuth.Application.Features.IpRestrictions.Models;
using ExoAuth.Domain.Enums;
using Mediator;

namespace ExoAuth.Application.Features.IpRestrictions.Commands.UpdateIpRestriction;

/// <summary>
/// Command to update an existing IP restriction.
/// </summary>
public sealed record UpdateIpRestrictionCommand(
    Guid Id,
    IpRestrictionType Type,
    string Reason,
    DateTime? ExpiresAt,
    Guid CurrentUserId
) : ICommand<IpRestrictionDto>;
