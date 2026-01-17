using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Queries.GetDevices;

/// <summary>
/// Query to get all devices for the current user.
/// </summary>
public sealed record GetDevicesQuery() : IQuery<List<DeviceDto>>;
