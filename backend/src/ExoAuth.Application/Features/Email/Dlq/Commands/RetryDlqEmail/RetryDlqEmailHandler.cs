using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Dlq.Commands.RetryDlqEmail;

public sealed class RetryDlqEmailHandler : ICommandHandler<RetryDlqEmailCommand, EmailLogDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;

    public RetryDlqEmailHandler(IAppDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async ValueTask<EmailLogDto> Handle(RetryDlqEmailCommand command, CancellationToken ct)
    {
        var log = await _dbContext.EmailLogs
            .Include(x => x.RecipientUser)
            .Include(x => x.SentViaProvider)
            .FirstOrDefaultAsync(x => x.Id == command.EmailLogId, ct);

        if (log is null)
        {
            throw new EmailLogNotFoundException(command.EmailLogId);
        }

        if (log.Status != EmailStatus.InDlq)
        {
            throw new EmailNotInDlqException(command.EmailLogId);
        }

        // Mark as retried and requeue
        log.Requeue();
        await _dbContext.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            "EMAIL_DLQ_RETRY",
            userId: null,
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

        // TODO: Re-queue the email to RabbitMQ for actual sending
        // This would typically be done via IMessageBus.PublishAsync()

        return new EmailLogDto(
            log.Id,
            log.RecipientUserId,
            log.RecipientEmail,
            log.RecipientUser != null ? $"{log.RecipientUser.FirstName} {log.RecipientUser.LastName}" : null,
            log.Subject,
            log.TemplateName,
            log.Language,
            log.Status,
            log.RetryCount,
            log.LastError,
            log.SentViaProviderId,
            log.SentViaProvider?.Name,
            log.QueuedAt,
            log.SentAt,
            log.FailedAt,
            log.MovedToDlqAt,
            log.AnnouncementId,
            log.CreatedAt);
    }
}
