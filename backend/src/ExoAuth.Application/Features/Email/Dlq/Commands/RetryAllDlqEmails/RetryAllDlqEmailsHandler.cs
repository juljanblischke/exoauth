using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Messages;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Dlq.Commands.RetryAllDlqEmails;

public sealed class RetryAllDlqEmailsHandler : ICommandHandler<RetryAllDlqEmailsCommand, RetryAllDlqEmailsResult>
{
    private readonly IAppDbContext _dbContext;
    private readonly IMessageBus _messageBus;
    private readonly IAuditService _auditService;

    private const string EmailRoutingKey = "email.send";

    public RetryAllDlqEmailsHandler(
        IAppDbContext dbContext,
        IMessageBus messageBus,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _messageBus = messageBus;
        _auditService = auditService;
    }

    public async ValueTask<RetryAllDlqEmailsResult> Handle(RetryAllDlqEmailsCommand command, CancellationToken ct)
    {
        var dlqEmails = await _dbContext.EmailLogs
            .Include(x => x.Announcement)
            .Where(x => x.Status == EmailStatus.InDlq)
            .ToListAsync(ct);

        foreach (var log in dlqEmails)
        {
            log.Requeue();
        }

        await _dbContext.SaveChangesAsync(ct);

        // Re-queue each email to RabbitMQ
        foreach (var log in dlqEmails)
        {
            var variables = !string.IsNullOrEmpty(log.TemplateVariables)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(log.TemplateVariables) ?? new Dictionary<string, string>()
                : new Dictionary<string, string>();

            // For announcements, include the raw HTML body
            string? htmlBody = null;
            string? plainTextBody = null;
            if (log.Announcement is not null)
            {
                htmlBody = log.Announcement.HtmlBody;
                plainTextBody = log.Announcement.PlainTextBody;
            }

            var message = new SendEmailMessage(
                To: log.RecipientEmail,
                Subject: log.Subject,
                TemplateName: log.TemplateName,
                Language: log.Language,
                Variables: variables,
                RecipientUserId: log.RecipientUserId,
                AnnouncementId: log.AnnouncementId,
                HtmlBody: htmlBody,
                PlainTextBody: plainTextBody,
                ExistingEmailLogId: log.Id
            );

            await _messageBus.PublishAsync(message, EmailRoutingKey, ct);
        }

        // Audit log
        await _auditService.LogAsync(
            "EMAIL_DLQ_RETRY_ALL",
            userId: null,
            targetUserId: null,
            entityType: "EmailLog",
            entityId: null,
            details: new { Count = dlqEmails.Count },
            cancellationToken: ct);

        return new RetryAllDlqEmailsResult(dlqEmails.Count);
    }
}
