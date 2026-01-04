using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Queries.GetTrustedDevices;

/// <summary>
/// Query to get all trusted devices for the current user.
/// </summary>
public sealed record GetTrustedDevicesQuery() : IQuery<List<TrustedDeviceDto>>;
