using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemInvites.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemInvites.Queries.GetSystemInvite;

public sealed class GetSystemInviteHandler : IQueryHandler<GetSystemInviteQuery, SystemInviteDetailDto?>
{
    private readonly IAppDbContext _context;

    public GetSystemInviteHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async ValueTask<SystemInviteDetailDto?> Handle(GetSystemInviteQuery query, CancellationToken ct)
    {
        var invite = await _context.SystemInvites
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.Id == query.Id, ct);

        if (invite is null)
            return null;

        // Get permission details
        var permissionIds = invite.GetPermissionIds();
        var permissions = await _context.SystemPermissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => new InvitePermissionDto(p.Name, p.Description))
            .ToListAsync(ct);

        return new SystemInviteDetailDto(
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
            ),
            Permissions: permissions
        );
    }
}
