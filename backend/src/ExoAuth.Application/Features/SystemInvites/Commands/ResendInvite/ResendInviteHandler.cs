using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemInvites.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemInvites.Commands.ResendInvite;

public sealed class ResendInviteHandler : ICommandHandler<ResendInviteCommand, SystemInviteListDto>
{
    private const int ResendCooldownMinutes = 5;

    private readonly IAppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUser;
    private readonly ISystemUserRepository _userRepository;
    private readonly IAuditService _auditService;

    public ResendInviteHandler(
        IAppDbContext context,
        IEmailService emailService,
        ICurrentUserService currentUser,
        ISystemUserRepository userRepository,
        IAuditService auditService)
    {
        _context = context;
        _emailService = emailService;
        _currentUser = currentUser;
        _userRepository = userRepository;
        _auditService = auditService;
    }

    public async ValueTask<SystemInviteListDto> Handle(ResendInviteCommand command, CancellationToken ct)
    {
        var invite = await _context.SystemInvites
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.Id == command.Id, ct);

        if (invite is null)
        {
            throw new NotFoundException("INVITE_NOT_FOUND", "Invitation not found");
        }

        if (invite.IsAccepted)
        {
            throw new BusinessException("INVITE_ALREADY_ACCEPTED", "Cannot resend accepted invitation", 400);
        }

        if (invite.IsRevoked)
        {
            throw new BusinessException("INVITE_ALREADY_REVOKED", "Cannot resend revoked invitation", 400);
        }

        // Check cooldown
        if (invite.ResentAt.HasValue)
        {
            var timeSinceResent = DateTime.UtcNow - invite.ResentAt.Value;
            if (timeSinceResent.TotalMinutes < ResendCooldownMinutes)
            {
                var remainingMinutes = (int)Math.Ceiling(ResendCooldownMinutes - timeSinceResent.TotalMinutes);
                throw new BusinessException(
                    "INVITE_RESEND_COOLDOWN",
                    $"Please wait {remainingMinutes} minute(s) before resending",
                    429);
            }
        }
        else
        {
            // Also check against CreatedAt for first resend
            var timeSinceCreated = DateTime.UtcNow - invite.CreatedAt;
            if (timeSinceCreated.TotalMinutes < ResendCooldownMinutes)
            {
                var remainingMinutes = (int)Math.Ceiling(ResendCooldownMinutes - timeSinceCreated.TotalMinutes);
                throw new BusinessException(
                    "INVITE_RESEND_COOLDOWN",
                    $"Please wait {remainingMinutes} minute(s) before resending",
                    429);
            }
        }

        // Get current user for resender name
        var resenderId = _currentUser.UserId
            ?? throw new AuthException("AUTH_UNAUTHORIZED", "User not authenticated", 401);

        var resender = await _userRepository.GetByIdAsync(resenderId, ct)
            ?? throw new AuthException("AUTH_UNAUTHORIZED", "User not found", 401);

        // Mark as resent (generates new token, extends expiration)
        invite.MarkResent();
        await _context.SaveChangesAsync(ct);

        // Send invitation email
        await _emailService.SendSystemInviteAsync(
            email: invite.Email,
            firstName: invite.FirstName,
            inviterName: resender.FullName,
            inviteToken: invite.Token,
            language: "en",
            cancellationToken: ct
        );

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.InviteResent,
            _currentUser.UserId,
            null,
            "SystemInvite",
            invite.Id,
            new { invite.Email, ResentBy = _currentUser.Email },
            ct
        );

        return new SystemInviteListDto(
            Id: invite.Id,
            Email: invite.Email,
            FirstName: invite.FirstName,
            LastName: invite.LastName,
            Status: invite.Status,
            ExpiresAt: invite.ExpiresAt,
            CreatedAt: invite.CreatedAt,
            AcceptedAt: invite.AcceptedAt,
            RevokedAt: invite.RevokedAt,
            ResentAt: invite.ResentAt,
            InvitedBy: new InvitedByDto(
                Id: invite.InvitedByUser.Id,
                Email: invite.InvitedByUser.Email,
                FullName: invite.InvitedByUser.FullName
            )
        );
    }
}
