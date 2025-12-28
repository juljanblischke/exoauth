using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemInvites.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemInvites.Queries.ValidateInvite;

public sealed class ValidateInviteHandler : IQueryHandler<ValidateInviteQuery, InviteValidationDto>
{
    private readonly IAppDbContext _context;
    private readonly ISystemInviteService _inviteService;

    public ValidateInviteHandler(IAppDbContext context, ISystemInviteService inviteService)
    {
        _context = context;
        _inviteService = inviteService;
    }

    public async ValueTask<InviteValidationDto> Handle(ValidateInviteQuery query, CancellationToken ct)
    {
        var invite = await _inviteService.ValidateTokenAsync(query.Token, ct);

        // Token not found
        if (invite is null)
        {
            return new InviteValidationDto(
                Valid: false,
                Email: null,
                FirstName: null,
                LastName: null,
                ExpiresAt: null,
                InvitedBy: null,
                Permissions: null,
                ErrorCode: ErrorCodes.AuthInviteInvalid,
                ErrorMessage: "Invalid invitation token"
            );
        }

        // Already accepted
        if (invite.IsAccepted)
        {
            return new InviteValidationDto(
                Valid: false,
                Email: invite.Email,
                FirstName: invite.FirstName,
                LastName: invite.LastName,
                ExpiresAt: invite.ExpiresAt,
                InvitedBy: new InviterDto(invite.InvitedByUser.FullName),
                Permissions: null,
                ErrorCode: "INVITE_ALREADY_ACCEPTED",
                ErrorMessage: "This invitation has already been accepted"
            );
        }

        // Revoked
        if (invite.IsRevoked)
        {
            return new InviteValidationDto(
                Valid: false,
                Email: invite.Email,
                FirstName: invite.FirstName,
                LastName: invite.LastName,
                ExpiresAt: invite.ExpiresAt,
                InvitedBy: new InviterDto(invite.InvitedByUser.FullName),
                Permissions: null,
                ErrorCode: "INVITE_REVOKED",
                ErrorMessage: "This invitation has been revoked"
            );
        }

        // Expired
        if (invite.IsExpired)
        {
            return new InviteValidationDto(
                Valid: false,
                Email: invite.Email,
                FirstName: invite.FirstName,
                LastName: invite.LastName,
                ExpiresAt: invite.ExpiresAt,
                InvitedBy: new InviterDto(invite.InvitedByUser.FullName),
                Permissions: null,
                ErrorCode: ErrorCodes.AuthInviteExpired,
                ErrorMessage: "This invitation has expired"
            );
        }

        // Valid - get permission details
        var permissionIds = invite.GetPermissionIds();
        var permissions = await _context.SystemPermissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => new InvitePermissionDto(p.Name, p.Description))
            .ToListAsync(ct);

        return new InviteValidationDto(
            Valid: true,
            Email: invite.Email,
            FirstName: invite.FirstName,
            LastName: invite.LastName,
            ExpiresAt: invite.ExpiresAt,
            InvitedBy: new InviterDto(invite.InvitedByUser.FullName),
            Permissions: permissions,
            ErrorCode: null,
            ErrorMessage: null
        );
    }
}
