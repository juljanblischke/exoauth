using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemInvites.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemInvites.Commands.RevokeInvite;

public sealed class RevokeInviteHandler : ICommandHandler<RevokeInviteCommand, SystemInviteListDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public RevokeInviteHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async ValueTask<SystemInviteListDto> Handle(RevokeInviteCommand command, CancellationToken ct)
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
            throw new BusinessException("INVITE_ALREADY_ACCEPTED", "Cannot revoke accepted invitation", 400);
        }

        if (invite.IsRevoked)
        {
            throw new BusinessException("INVITE_ALREADY_REVOKED", "Invitation already revoked", 400);
        }

        invite.Revoke();
        await _context.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.InviteRevoked,
            _currentUser.UserId,
            null,
            "SystemInvite",
            invite.Id,
            new { invite.Email, RevokedBy = _currentUser.Email },
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
