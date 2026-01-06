using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.DeletePasskey;

public sealed class DeletePasskeyHandler : ICommandHandler<DeletePasskeyCommand, bool>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;

    public DeletePasskeyHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IAuditService auditService,
        IEmailService emailService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditService = auditService;
        _emailService = emailService;
    }

    public async ValueTask<bool> Handle(DeletePasskeyCommand command, CancellationToken ct)
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

        var passkey = user.Passkeys.FirstOrDefault(p => p.Id == command.PasskeyId);

        if (passkey is null)
        {
            throw new PasskeyNotFoundException();
        }

        // Check if this is the last passkey and user has no password set
        var isLastPasskey = user.Passkeys.Count == 1;
        var hasPassword = user.PasswordHash != "ANONYMIZED" && !string.IsNullOrEmpty(user.PasswordHash);

        if (isLastPasskey && !hasPassword)
        {
            throw new PasskeyCannotDeleteLastException();
        }

        var passkeyName = passkey.Name;

        _context.Passkeys.Remove(passkey);
        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            AuditActions.PasskeyDeleted,
            userId,
            null,
            "Passkey",
            command.PasskeyId,
            new { PasskeyName = passkeyName },
            ct);

        // Send security alert email
        await _emailService.SendPasskeyRemovedEmailAsync(
            user.Email,
            user.FullName,
            passkeyName,
            user.PreferredLanguage,
            ct);

        return true;
    }
}
