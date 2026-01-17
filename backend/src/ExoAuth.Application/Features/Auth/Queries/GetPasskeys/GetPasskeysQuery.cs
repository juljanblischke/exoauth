using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Queries.GetPasskeys;

/// <summary>
/// Query to get all passkeys for the current user.
/// </summary>
public sealed record GetPasskeysQuery : IQuery<GetPasskeysResponse>;

/// <summary>
/// Response containing the user's passkeys.
/// </summary>
public sealed record GetPasskeysResponse(
    IReadOnlyList<PasskeyDto> Passkeys
);
