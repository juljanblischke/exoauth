using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemInvites.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemInvites.Commands.UpdateInvite;

public sealed class UpdateInviteHandler : ICommandHandler<UpdateInviteCommand, SystemInviteListDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public UpdateInviteHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async ValueTask<SystemInviteListDto> Handle(UpdateInviteCommand command, CancellationToken ct)
    {
        var invite = await _context.SystemInvites
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.Id == command.Id, ct);

        if (invite is null)
        {
            throw new NotFoundException("INVITE_NOT_FOUND", "Invitation not found");
        }

        // Only pending invites can be edited
        if (invite.Status != "pending")
        {
            throw new InviteNotEditableException(invite.Id);
        }

        // Validate permissions if provided
        if (command.PermissionIds is not null)
        {
            var validPermissionIds = await _context.SystemPermissions
                .Where(p => command.PermissionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(ct);

            var invalidIds = command.PermissionIds.Except(validPermissionIds).ToList();
            if (invalidIds.Count > 0)
            {
                throw new SystemPermissionNotFoundException(invalidIds.First());
            }
        }

        // Store old values for audit
        var oldValues = new
        {
            invite.FirstName,
            invite.LastName,
            PermissionIds = invite.GetPermissionIds()
        };

        // Update invite
        invite.Update(command.FirstName, command.LastName, command.PermissionIds);
        await _context.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.InviteUpdated,
            _currentUser.UserId,
            null,
            "SystemInvite",
            invite.Id,
            new
            {
                invite.Email,
                OldFirstName = oldValues.FirstName,
                NewFirstName = invite.FirstName,
                OldLastName = oldValues.LastName,
                NewLastName = invite.LastName,
                OldPermissionIds = oldValues.PermissionIds,
                NewPermissionIds = invite.GetPermissionIds(),
                UpdatedBy = _currentUser.Email
            },
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
