using ExoAuth.Application.Features.IpRestrictions.Models;
using ExoAuth.Domain.Enums;
using Mediator;

namespace ExoAuth.Application.Features.IpRestrictions.Commands.CreateIpRestriction;

/// <summary>
/// Command to create a new IP restriction (whitelist or blacklist entry).
/// </summary>
public sealed record CreateIpRestrictionCommand(
    string IpAddress,
    IpRestrictionType Type,
    string Reason,
    DateTime? ExpiresAt,
    Guid CurrentUserId
) : ICommand<IpRestrictionDto>;
