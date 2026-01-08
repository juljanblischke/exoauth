using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Dlq.Commands.RetryAllDlqEmails;

public sealed class RetryAllDlqEmailsHandler : ICommandHandler<RetryAllDlqEmailsCommand, RetryAllDlqEmailsResult>
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;

    public RetryAllDlqEmailsHandler(IAppDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async ValueTask<RetryAllDlqEmailsResult> Handle(RetryAllDlqEmailsCommand command, CancellationToken ct)
    {
        var dlqEmails = await _dbContext.EmailLogs
            .Where(x => x.Status == EmailStatus.InDlq)
            .ToListAsync(ct);

        foreach (var log in dlqEmails)
        {
            log.Requeue();
        }

        await _dbContext.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            "EMAIL_DLQ_RETRY_ALL",
            userId: null,
            targetUserId: null,
            entityType: "EmailLog",
            entityId: null,
            details: new { Count = dlqEmails.Count },
            cancellationToken: ct);

        // TODO: Re-queue emails to RabbitMQ for actual sending

        return new RetryAllDlqEmailsResult(dlqEmails.Count);
    }
}
