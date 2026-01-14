using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Commands.UnlockUser;

public sealed class UnlockUserHandler : ICommandHandler<UnlockUserCommand, UnlockUserResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IBruteForceProtectionService _bruteForceService;

    public UnlockUserHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IAuditService auditService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IBruteForceProtectionService bruteForceService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditService = auditService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _bruteForceService = bruteForceService;
    }

    public async ValueTask<UnlockUserResponse> Handle(UnlockUserCommand command, CancellationToken ct)
    {
        var adminUserId = _currentUser.UserId
            ?? throw new UnauthorizedException();

        var user = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
            ?? throw new SystemUserNotFoundException(command.UserId);

        if (!user.IsLocked && user.FailedLoginAttempts == 0)
        {
            // User is not locked, nothing to do
            return new UnlockUserResponse(true);
        }

        // Unlock user in database
        user.Unlock();

        // Reset Redis brute force counter
        await _bruteForceService.ResetAsync(user.Email, ct);

        await _context.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.AccountUnlockedByAdmin,
            adminUserId,
            command.UserId,
            "SystemUser",
            command.UserId,
            new { Reason = command.Reason },
            ct
        );

        // Send notification email to user
        await _emailService.SendAsync(
            user.Email,
            _emailTemplateService.GetSubject("account-unlocked", user.PreferredLanguage),
            "account-unlocked",
            new Dictionary<string, string>
            {
                ["firstName"] = user.FirstName,
                ["year"] = DateTime.UtcNow.Year.ToString()
            },
            user.PreferredLanguage,
            user.Id,
            ct
        );

        return new UnlockUserResponse(true);
    }
}
