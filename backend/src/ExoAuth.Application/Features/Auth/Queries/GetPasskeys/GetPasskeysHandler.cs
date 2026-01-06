using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Queries.GetPasskeys;

public sealed class GetPasskeysHandler : IQueryHandler<GetPasskeysQuery, GetPasskeysResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetPasskeysHandler(
        IAppDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async ValueTask<GetPasskeysResponse> Handle(GetPasskeysQuery query, CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        var userId = _currentUser.UserId.Value;

        var passkeys = await _context.Passkeys
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => PasskeyDto.FromEntity(p))
            .ToListAsync(ct);

        return new GetPasskeysResponse(passkeys);
    }
}
