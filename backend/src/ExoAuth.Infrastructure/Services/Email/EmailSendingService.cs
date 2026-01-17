using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services.Email;

public sealed class EmailSendingService : IEmailSendingService
{
    private readonly IAppDbContext _dbContext;
    private readonly IEmailProviderFactory _providerFactory;
    private readonly ICircuitBreakerService _circuitBreaker;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<EmailSendingService> _logger;

    public EmailSendingService(
        IAppDbContext dbContext,
        IEmailProviderFactory providerFactory,
        ICircuitBreakerService circuitBreaker,
        IDateTimeProvider dateTimeProvider,
        ILogger<EmailSendingService> logger)
    {
        _dbContext = dbContext;
        _providerFactory = providerFactory;
        _circuitBreaker = circuitBreaker;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendWithFailoverAsync(
        string recipientEmail,
        Guid? recipientUserId,
        string subject,
        string htmlBody,
        string? plainTextBody,
        string templateName,
        string? templateVariables,
        string language,
        Guid? announcementId = null,
        Guid? existingEmailLogId = null,
        CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(cancellationToken);

        // Check if emails are enabled
        if (!config.EmailsEnabled)
        {
            _logger.LogWarning("Email sending is disabled globally");
            return new EmailSendResult(false, Guid.Empty, null, "Email sending is disabled");
        }

        // Track if this is a DLQ retry (for announcement count updates)
        var isDlqRetry = existingEmailLogId.HasValue;

        // Either fetch existing log (DLQ retry) or create new one
        EmailLog emailLog;
        if (existingEmailLogId.HasValue)
        {
            emailLog = await _dbContext.EmailLogs
                .FirstOrDefaultAsync(x => x.Id == existingEmailLogId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"EmailLog {existingEmailLogId} not found");

            // Use the announcement ID from the existing log if not provided
            announcementId ??= emailLog.AnnouncementId;

            // Reset for retry
            emailLog.MarkSending();
        }
        else
        {
            // Create new email log
            emailLog = EmailLog.Create(
                recipientEmail,
                subject,
                templateName,
                language,
                recipientUserId,
                templateVariables,
                announcementId);

            _dbContext.EmailLogs.Add(emailLog);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Test mode - just log, don't send
        if (config.TestMode)
        {
            _logger.LogInformation("Test mode: Email logged but not sent to {Recipient}", recipientEmail);
            return new EmailSendResult(true, emailLog.Id, null, null);
        }

        // Get enabled providers sorted by priority
        var providers = await _dbContext.EmailProviders
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);

        if (providers.Count == 0)
        {
            emailLog.MarkFailed("No email providers configured");
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new EmailSendResult(false, emailLog.Id, null, "No email providers configured");
        }

        emailLog.MarkSending();
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Try each provider with retries
        foreach (var provider in providers)
        {
            if (!await _circuitBreaker.CanUseProviderAsync(provider, cancellationToken))
            {
                _logger.LogDebug("Skipping provider {ProviderId} due to circuit breaker", provider.Id);
                continue;
            }

            var success = await TrySendWithRetriesAsync(
                provider, recipientEmail, subject, htmlBody, plainTextBody,
                emailLog, config, cancellationToken);

            if (success)
            {
                emailLog.MarkSent(provider.Id);

                // Update announcement counts if applicable
                if (announcementId.HasValue)
                {
                    var announcement = await _dbContext.EmailAnnouncements
                        .FirstOrDefaultAsync(x => x.Id == announcementId.Value, cancellationToken);

                    if (announcement is not null)
                    {
                        if (isDlqRetry)
                        {
                            // DLQ retry succeeded: move from failed to sent
                            // (FailedCount was already incremented when first moved to DLQ)
                            // Note: We don't decrement FailedCount below 0
                            var newFailedCount = Math.Max(0, announcement.FailedCount - 1);
                            announcement.UpdateStats(announcement.SentCount + 1, newFailedCount);
                        }
                        else
                        {
                            // First-time success
                            announcement.IncrementSentCount();
                        }
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Email sent successfully via {ProviderName} to {Recipient}",
                    provider.Name, recipientEmail);

                return new EmailSendResult(true, emailLog.Id, provider.Id, null);
            }
        }

        // All providers failed - move to DLQ
        emailLog.MoveToDlq(emailLog.LastError ?? "All providers exhausted");

        // Update announcement counts if applicable (only for first-time failures, not DLQ retries)
        if (announcementId.HasValue && !isDlqRetry)
        {
            var announcement = await _dbContext.EmailAnnouncements
                .FirstOrDefaultAsync(x => x.Id == announcementId.Value, cancellationToken);

            if (announcement is not null)
            {
                announcement.IncrementFailedCount();
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogError(
            "All email providers exhausted for email {EmailLogId}. Moved to DLQ.",
            emailLog.Id);

        return new EmailSendResult(false, emailLog.Id, null, "All providers exhausted - email moved to DLQ");
    }

    private async Task<bool> TrySendWithRetriesAsync(
        EmailProvider provider,
        string recipientEmail,
        string subject,
        string htmlBody,
        string? plainTextBody,
        EmailLog emailLog,
        EmailConfiguration config,
        CancellationToken cancellationToken)
    {
        var providerImplementation = _providerFactory.CreateProvider(provider);

        for (int retry = 0; retry < config.MaxRetriesPerProvider; retry++)
        {
            try
            {
                await providerImplementation.SendAsync(
                    recipientEmail, subject, htmlBody, plainTextBody, cancellationToken);

                await _circuitBreaker.RecordSuccessAsync(provider, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                emailLog.RecordRetryAttempt(ex.Message);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(ex,
                    "Provider {ProviderId} failed attempt {Attempt}/{MaxRetries} for email {EmailLogId}",
                    provider.Id, retry + 1, config.MaxRetriesPerProvider, emailLog.Id);

                if (retry < config.MaxRetriesPerProvider)
                {
                    var delay = CalculateBackoff(retry, config);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        // Provider exhausted all retries
        await _circuitBreaker.RecordFailureAsync(provider, cancellationToken);
        return false;
    }

    private static int CalculateBackoff(int retryAttempt, EmailConfiguration config)
    {
        var delay = config.InitialRetryDelayMs * Math.Pow(config.BackoffMultiplier, retryAttempt);
        return Math.Min((int)delay, config.MaxRetryDelayMs);
    }

    private async Task<EmailConfiguration> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        var config = await _dbContext.EmailConfigurations.FirstOrDefaultAsync(cancellationToken);
        return config ?? EmailConfiguration.CreateDefault();
    }
}
