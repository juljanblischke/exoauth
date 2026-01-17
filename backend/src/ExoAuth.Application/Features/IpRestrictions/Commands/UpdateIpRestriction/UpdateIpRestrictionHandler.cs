using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.IpRestrictions.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.IpRestrictions.Commands.UpdateIpRestriction;

public sealed class UpdateIpRestrictionHandler : ICommandHandler<UpdateIpRestrictionCommand, IpRestrictionDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IIpRestrictionService _ipRestrictionService;
    private readonly IAuditService _auditService;

    public UpdateIpRestrictionHandler(
        IAppDbContext dbContext,
        IIpRestrictionService ipRestrictionService,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _ipRestrictionService = ipRestrictionService;
        _auditService = auditService;
    }

    public async ValueTask<IpRestrictionDto> Handle(UpdateIpRestrictionCommand command, CancellationToken ct)
    {
        var restriction = await _dbContext.IpRestrictions
            .FirstOrDefaultAsync(x => x.Id == command.Id, ct);

        if (restriction == null)
        {
            throw new IpRestrictionNotFoundException(command.Id);
        }

        // Capture old values for audit
        var oldType = restriction.Type;
        var oldReason = restriction.Reason;
        var oldExpiresAt = restriction.ExpiresAt;

        // Update the restriction
        restriction.Update(command.Type, command.Reason, command.ExpiresAt);

        await _dbContext.SaveChangesAsync(ct);

        // Invalidate cache
        await _ipRestrictionService.InvalidateCacheAsync(ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.IpRestrictionUpdated,
            userId: command.CurrentUserId,
            targetUserId: null,
            entityType: "IpRestriction",
            entityId: restriction.Id,
            details: new
            {
                restriction.IpAddress,
                OldType = oldType.ToString(),
                NewType = command.Type.ToString(),
                OldReason = oldReason,
                NewReason = command.Reason,
                OldExpiresAt = oldExpiresAt,
                NewExpiresAt = command.ExpiresAt
            },
            cancellationToken: ct);

        // Get created by user info
        var createdByUser = restriction.CreatedByUserId.HasValue
            ? await _dbContext.SystemUsers
                .Where(x => x.Id == restriction.CreatedByUserId.Value)
                .Select(x => new { x.Email, x.FirstName, x.LastName })
                .FirstOrDefaultAsync(ct)
            : null;

        return new IpRestrictionDto(
            restriction.Id,
            restriction.IpAddress,
            restriction.Type,
            restriction.Reason,
            restriction.Source,
            restriction.ExpiresAt,
            restriction.CreatedAt,
            restriction.CreatedByUserId,
            createdByUser?.Email,
            createdByUser != null ? $"{createdByUser.FirstName} {createdByUser.LastName}" : null);
    }
}
