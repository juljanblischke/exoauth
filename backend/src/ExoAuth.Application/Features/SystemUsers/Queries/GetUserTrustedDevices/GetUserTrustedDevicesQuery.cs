using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Queries.GetUserTrustedDevices;

/// <summary>
/// Query to get all trusted devices for a specific user (admin action).
/// </summary>
/// <param name="UserId">The user ID to get devices for.</param>
public sealed record GetUserTrustedDevicesQuery(Guid UserId) : IQuery<List<TrustedDeviceDto>>;
