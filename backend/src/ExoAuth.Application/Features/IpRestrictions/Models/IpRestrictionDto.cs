using ExoAuth.Domain.Enums;

namespace ExoAuth.Application.Features.IpRestrictions.Models;

/// <summary>
/// IP restriction list item and detail DTO.
/// </summary>
public sealed record IpRestrictionDto(
    Guid Id,
    string IpAddress,
    IpRestrictionType Type,
    string Reason,
    IpRestrictionSource Source,
    DateTime? ExpiresAt,
    DateTime CreatedAt,
    Guid? CreatedByUserId,
    string? CreatedByUserEmail,
    string? CreatedByUserFullName
);

/// <summary>
/// Request to create a new IP restriction.
/// </summary>
public sealed record CreateIpRestrictionRequest(
    string IpAddress,
    IpRestrictionType Type,
    string Reason,
    DateTime? ExpiresAt
);

/// <summary>
/// Request to update an existing IP restriction.
/// </summary>
public sealed record UpdateIpRestrictionRequest(
    IpRestrictionType Type,
    string Reason,
    DateTime? ExpiresAt
);
