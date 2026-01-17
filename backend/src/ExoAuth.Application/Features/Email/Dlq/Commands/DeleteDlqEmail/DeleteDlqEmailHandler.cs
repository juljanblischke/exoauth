using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Dlq.Commands.DeleteDlqEmail;

public sealed class DeleteDlqEmailHandler : ICommandHandler<DeleteDlqEmailCommand>
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public DeleteDlqEmailHandler(
        IAppDbContext dbContext,
        IAuditService auditService,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(DeleteDlqEmailCommand command, CancellationToken ct)
    {
        var log = await _dbContext.EmailLogs
            .FirstOrDefaultAsync(x => x.Id == command.EmailLogId, ct);

        if (log is null)
        {
            throw new EmailLogNotFoundException(command.EmailLogId);
        }

        if (log.Status != EmailStatus.InDlq)
        {
            throw new EmailNotInDlqException(command.EmailLogId);
        }

        // Mark as permanently failed instead of deleting to keep history
        log.MarkFailed("Manually removed from DLQ");
        await _dbContext.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.EmailDlqDeleted,
            userId: _currentUser.UserId,
            targetUserId: log.RecipientUserId,
            entityType: "EmailLog",
            entityId: log.Id,
            details: new
            {
                log.RecipientEmail,
                log.Subject,
                log.TemplateName
            },
            cancellationToken: ct);

        return Unit.Value;
    }
}
