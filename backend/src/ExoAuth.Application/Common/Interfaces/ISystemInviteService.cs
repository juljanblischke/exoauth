using ExoAuth.Domain.Entities;

namespace ExoAuth.Application.Common.Interfaces;

public sealed record InviteTokenResult(string Token, string TokenHash);

public interface ISystemInviteService
{
    Task<InviteTokenResult> GenerateTokenAsync(CancellationToken cancellationToken = default);

    Task<SystemInvite?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}
