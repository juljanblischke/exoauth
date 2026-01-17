using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.PasskeyRegisterOptions;

public sealed class PasskeyRegisterOptionsHandler : ICommandHandler<PasskeyRegisterOptionsCommand, PasskeyRegisterOptionsResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasskeyService _passkeyService;

    public PasskeyRegisterOptionsHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IPasskeyService passkeyService)
    {
        _context = context;
        _currentUser = currentUser;
        _passkeyService = passkeyService;
    }

    public async ValueTask<PasskeyRegisterOptionsResponse> Handle(PasskeyRegisterOptionsCommand command, CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        var userId = _currentUser.UserId.Value;

        var user = await _context.SystemUsers
            .Include(u => u.Passkeys)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException();

        if (!user.IsActive)
        {
            throw new UserInactiveException();
        }

        if (user.IsLocked)
        {
            throw new AccountLockedException(user.LockedUntil);
        }

        // Get existing credential IDs to exclude (prevent re-registration)
        var existingCredentialIds = user.Passkeys
            .Select(p => p.CredentialId)
            .ToList();

        var (options, challengeId) = await _passkeyService.CreateRegistrationOptionsAsync(
            user,
            existingCredentialIds,
            ct);

        return new PasskeyRegisterOptionsResponse(options, challengeId);
    }
}
