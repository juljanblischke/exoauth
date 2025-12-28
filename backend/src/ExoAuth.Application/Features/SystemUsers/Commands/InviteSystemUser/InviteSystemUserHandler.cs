using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Models;
using ExoAuth.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Commands.InviteSystemUser;

public sealed class InviteSystemUserHandler : ICommandHandler<InviteSystemUserCommand, SystemInviteDto>
{
    private readonly IAppDbContext _context;
    private readonly ISystemUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;
    private readonly ISystemInviteService _inviteService;

    public InviteSystemUserHandler(
        IAppDbContext context,
        ISystemUserRepository userRepository,
        IEmailService emailService,
        ICurrentUserService currentUser,
        IAuditService auditService,
        ISystemInviteService inviteService)
    {
        _context = context;
        _userRepository = userRepository;
        _emailService = emailService;
        _currentUser = currentUser;
        _auditService = auditService;
        _inviteService = inviteService;
    }

    public async ValueTask<SystemInviteDto> Handle(InviteSystemUserCommand command, CancellationToken ct)
    {
        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(command.Email, ct))
        {
            throw new EmailExistsException();
        }

        // Check if there's already a pending invite for this email
        var existingInvite = await _context.SystemInvites
            .FirstOrDefaultAsync(i => i.Email == command.Email.ToLowerInvariant()
                && i.AcceptedAt == null
                && i.ExpiresAt > DateTime.UtcNow, ct);

        if (existingInvite is not null)
        {
            throw new AuthException("AUTH_INVITE_PENDING",
                "An invitation is already pending for this email", 409);
        }

        // Validate permissions exist
        var validPermissionIds = await _context.SystemPermissions
            .Where(p => command.PermissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);

        var invalidPermissions = command.PermissionIds.Except(validPermissionIds).ToList();
        if (invalidPermissions.Count > 0)
        {
            throw new SystemPermissionNotFoundException(invalidPermissions.First());
        }

        // Get current user for inviter name
        var inviterId = _currentUser.UserId
            ?? throw new AuthException("AUTH_UNAUTHORIZED", "User not authenticated", 401);

        var inviter = await _userRepository.GetByIdAsync(inviterId, ct)
            ?? throw new AuthException("AUTH_UNAUTHORIZED", "User not found", 401);

        // Generate token with collision prevention
        var tokenResult = await _inviteService.GenerateTokenAsync(ct);

        // Create invite
        var invite = SystemInvite.Create(
            email: command.Email,
            firstName: command.FirstName,
            lastName: command.LastName,
            permissionIds: command.PermissionIds,
            invitedBy: inviterId,
            tokenHash: tokenResult.TokenHash
        );

        await _context.SystemInvites.AddAsync(invite, ct);
        await _context.SaveChangesAsync(ct);

        // Send invitation email
        await _emailService.SendSystemInviteAsync(
            email: invite.Email,
            firstName: invite.FirstName,
            inviterName: inviter.FullName,
            inviteToken: tokenResult.Token,
            language: "en", // TODO: Could be based on user preference
            cancellationToken: ct
        );

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.UserInvited,
            inviterId,
            null, // targetUserId - no user yet
            "SystemInvite",
            invite.Id,
            new { invite.Email, PermissionCount = command.PermissionIds.Count },
            ct
        );

        return new SystemInviteDto(
            Id: invite.Id,
            Email: invite.Email,
            FirstName: invite.FirstName,
            LastName: invite.LastName,
            ExpiresAt: invite.ExpiresAt,
            CreatedAt: invite.CreatedAt
        );
    }
}
