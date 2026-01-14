using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.PasskeyRegister;

public sealed class PasskeyRegisterHandler : ICommandHandler<PasskeyRegisterCommand, PasskeyDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasskeyService _passkeyService;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;

    public PasskeyRegisterHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IPasskeyService passkeyService,
        IAuditService auditService,
        IEmailService emailService)
    {
        _context = context;
        _currentUser = currentUser;
        _passkeyService = passkeyService;
        _auditService = auditService;
        _emailService = emailService;
    }

    public async ValueTask<PasskeyDto> Handle(PasskeyRegisterCommand command, CancellationToken ct)
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

        // Verify registration response
        var credential = await _passkeyService.VerifyRegistrationAsync(
            userId,
            command.ChallengeId,
            command.AttestationResponse,
            ct);

        if (credential is null)
        {
            throw new PasskeyRegistrationFailedException();
        }

        // Check if credential already exists (shouldn't happen due to excludeCredentials, but safety check)
        var credentialExists = await _context.Passkeys
            .AnyAsync(p => p.CredentialId.SequenceEqual(credential.Id), ct);

        if (credentialExists)
        {
            throw new PasskeyAlreadyRegisteredException();
        }

        // Create passkey entity
        var passkey = Passkey.Create(
            userId: userId,
            credentialId: credential.Id,
            publicKey: credential.PublicKey,
            counter: credential.Counter,
            credType: credential.Type,
            aaGuid: credential.AaGuid,
            name: command.Name);

        _context.Passkeys.Add(passkey);
        await _context.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.PasskeyRegistered,
            userId,
            null,
            "Passkey",
            passkey.Id,
            new { passkey.Name },
            ct);

        // Send email notification
        await _emailService.SendPasskeyRegisteredEmailAsync(
            user.Email,
            user.FullName,
            passkey.Name,
            user.Id,
            user.PreferredLanguage,
            ct);

        return PasskeyDto.FromEntity(passkey);
    }
}
