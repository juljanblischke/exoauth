using System.Text.Json;
using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Messages;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Dlq.Commands.RetryDlqEmail;

public sealed class RetryDlqEmailHandler : ICommandHandler<RetryDlqEmailCommand, EmailLogDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IMessageBus _messageBus;
    private readonly IAuditService _auditService;

    private const string EmailRoutingKey = "email.send";

    public RetryDlqEmailHandler(
        IAppDbContext dbContext,
        IMessageBus messageBus,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _messageBus = messageBus;
        _auditService = auditService;
    }

    public async ValueTask<EmailLogDto> Handle(RetryDlqEmailCommand command, CancellationToken ct)
    {
        var log = await _dbContext.EmailLogs
            .Include(x => x.RecipientUser)
            .Include(x => x.SentViaProvider)
            .Include(x => x.Announcement)
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

        // Re-queue the email to RabbitMQ
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
