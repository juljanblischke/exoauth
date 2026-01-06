using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.PasskeyLoginOptions;

public sealed class PasskeyLoginOptionsHandler : ICommandHandler<PasskeyLoginOptionsCommand, PasskeyLoginOptionsResponse>
{
    private readonly IAppDbContext _context;
    private readonly IPasskeyService _passkeyService;

    public PasskeyLoginOptionsHandler(
        IAppDbContext context,
        IPasskeyService passkeyService)
    {
        _context = context;
        _passkeyService = passkeyService;
    }

    public async ValueTask<PasskeyLoginOptionsResponse> Handle(PasskeyLoginOptionsCommand command, CancellationToken ct)
    {
        IEnumerable<byte[]>? allowedCredentialIds = null;

        // If email is provided, get only that user's passkeys
        if (!string.IsNullOrEmpty(command.Email))
        {
            var email = command.Email.ToLowerInvariant();
            var user = await _context.SystemUsers
                .Include(u => u.Passkeys)
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);

            if (user is not null && user.Passkeys.Count > 0)
            {
                allowedCredentialIds = user.Passkeys.Select(p => p.CredentialId).ToList();
            }
        }

        var (options, challengeId) = await _passkeyService.CreateLoginOptionsAsync(
            allowedCredentialIds,
            ct);

        return new PasskeyLoginOptionsResponse(options, challengeId);
    }
}
