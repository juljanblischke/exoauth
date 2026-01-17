using Mediator;

namespace ExoAuth.Application.Features.IpRestrictions.Commands.DeleteIpRestriction;

/// <summary>
/// Command to delete an IP restriction.
/// </summary>
public sealed record DeleteIpRestrictionCommand(Guid Id) : ICommand;
