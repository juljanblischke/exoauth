using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.IpRestrictions.Commands.DeleteIpRestriction;

public sealed class DeleteIpRestrictionHandler : ICommandHandler<DeleteIpRestrictionCommand>
{
    private readonly IAppDbContext _dbContext;
    private readonly IIpRestrictionService _ipRestrictionService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteIpRestrictionHandler(
        IAppDbContext dbContext,
        IIpRestrictionService ipRestrictionService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _ipRestrictionService = ipRestrictionService;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async ValueTask<Unit> Handle(DeleteIpRestrictionCommand command, CancellationToken ct)
    {
        var restriction = await _dbContext.IpRestrictions
            .FirstOrDefaultAsync(x => x.Id == command.Id, ct);

        if (restriction == null)
        {
            throw new IpRestrictionNotFoundException(command.Id);
        }

        // Capture data for audit before deletion
        var auditData = new
        {
            restriction.IpAddress,
            Type = restriction.Type.ToString(),
            restriction.Reason,
            restriction.Source
        };

        _dbContext.IpRestrictions.Remove(restriction);
        await _dbContext.SaveChangesAsync(ct);

        // Invalidate cache
        await _ipRestrictionService.InvalidateCacheAsync(ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.IpRestrictionDeleted,
            userId: _currentUserService.UserId,
            targetUserId: null,
            entityType: "IpRestriction",
            entityId: command.Id,
            details: auditData,
            cancellationToken: ct);

        return Unit.Value;
    }
}
