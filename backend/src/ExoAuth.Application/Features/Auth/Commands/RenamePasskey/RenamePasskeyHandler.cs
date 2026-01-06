using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.RenamePasskey;

public sealed class RenamePasskeyHandler : ICommandHandler<RenamePasskeyCommand, PasskeyDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public RenamePasskeyHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async ValueTask<PasskeyDto> Handle(RenamePasskeyCommand command, CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        var userId = _currentUser.UserId.Value;

        var passkey = await _context.Passkeys
            .FirstOrDefaultAsync(p => p.Id == command.PasskeyId && p.UserId == userId, ct);

        if (passkey is null)
        {
            throw new PasskeyNotFoundException();
        }

        var oldName = passkey.Name;
        passkey.Rename(command.Name);
        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            AuditActions.PasskeyRenamed,
            userId,
            null,
            "Passkey",
            passkey.Id,
            new { OldName = oldName, NewName = command.Name },
            ct);

        return PasskeyDto.FromEntity(passkey);
    }
}
