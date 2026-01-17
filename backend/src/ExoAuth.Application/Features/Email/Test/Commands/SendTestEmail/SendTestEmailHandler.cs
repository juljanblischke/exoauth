using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Email.Test.Commands.SendTestEmail;

public sealed class SendTestEmailHandler(
    IAppDbContext dbContext,
    IEmailProviderFactory providerFactory,
    ICircuitBreakerService circuitBreaker,
    ILogger<SendTestEmailHandler> logger
) : IRequestHandler<SendTestEmailCommand, TestEmailResultDto>
{
    private const string TestEmailTemplateName = "test-email";
    private const string TestEmailSubject = "ExoAuth Test Email";
    private const string TestEmailHtmlBody = """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>ExoAuth Test Email</title>
        </head>
        <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">
            <h1 style="color: #333;">ExoAuth Test Email</h1>
            <p>This is a test email to verify your email provider configuration is working correctly.</p>
            <p style="color: #666; font-size: 14px;">If you received this email, your email provider is configured properly.</p>
            <hr style="border: none; border-top: 1px solid #eee; margin: 20px 0;">
            <p style="color: #999; font-size: 12px;">Sent by ExoAuth Email System</p>
        </body>
        </html>
        """;
    private const string TestEmailPlainTextBody = """
        ExoAuth Test Email
        
        This is a test email to verify your email provider configuration is working correctly.
        
        If you received this email, your email provider is configured properly.
        
        --
        Sent by ExoAuth Email System
        """;

    public async ValueTask<TestEmailResultDto> Handle(SendTestEmailCommand request, CancellationToken cancellationToken)
    {
        var providersQuery = dbContext.EmailProviders
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority);

        // Create email log entry
        var emailLog = EmailLog.Create(
            recipientEmail: request.RecipientEmail,
            subject: TestEmailSubject,
            templateName: TestEmailTemplateName,
            language: "en-US"
        );
        dbContext.EmailLogs.Add(emailLog);
        await dbContext.SaveChangesAsync(cancellationToken);

        // If a specific provider is requested, test only that one
        if (request.ProviderId.HasValue)
        {
            var provider = await dbContext.EmailProviders
                .FirstOrDefaultAsync(p => p.Id == request.ProviderId.Value, cancellationToken);

            if (provider is null)
            {
                emailLog.MarkFailed("Provider not found");
                await dbContext.SaveChangesAsync(cancellationToken);
                throw new EmailProviderNotFoundException(request.ProviderId!.Value);
            }

            emailLog.MarkSending();
            await dbContext.SaveChangesAsync(cancellationToken);

            var result = await SendTestEmailViaProviderAsync(provider, request.RecipientEmail, emailLog, cancellationToken);

            if (result.Success)
            {
                emailLog.MarkSent(provider.Id);
            }
            else
            {
                emailLog.MarkFailed(result.Error ?? "Unknown error");
            }
            await dbContext.SaveChangesAsync(cancellationToken);

            return result with { TotalProvidersAttempted = 1 };
        }

        // Test with failover through all enabled providers
        var providers = await providersQuery.ToListAsync(cancellationToken);

        if (providers.Count == 0)
        {
            emailLog.MarkFailed("No email providers configured");
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new EmailNoProvidersConfiguredException();
        }

        emailLog.MarkSending();
        await dbContext.SaveChangesAsync(cancellationToken);

        var attemptCount = 0;
        string? lastError = null;

        foreach (var provider in providers)
        {
            attemptCount++;

            // Check circuit breaker
            if (!await circuitBreaker.CanUseProviderAsync(provider, cancellationToken))
            {
                logger.LogDebug("Skipping provider {ProviderName} due to open circuit breaker", provider.Name);
                lastError = $"Provider {provider.Name}: circuit breaker open";
                continue;
            }

            var result = await SendTestEmailViaProviderAsync(provider, request.RecipientEmail, emailLog, cancellationToken);

            if (result.Success)
            {
                emailLog.MarkSent(provider.Id);
                await dbContext.SaveChangesAsync(cancellationToken);
                return result with { AttemptCount = attemptCount, TotalProvidersAttempted = providers.Count };
            }

            lastError = result.Error;
            emailLog.RecordRetryAttempt(lastError ?? "Unknown error");
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // All providers failed
        emailLog.MarkFailed(lastError ?? "All providers failed to send test email");
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TestEmailResultDto(
            Success: false,
            Error: lastError ?? "All providers failed to send test email",
            ProviderUsedId: null,
            ProviderUsedName: null,
            AttemptCount: attemptCount,
            TotalProvidersAttempted: providers.Count
        );
    }

    private async ValueTask<TestEmailResultDto> SendTestEmailViaProviderAsync(
        EmailProvider provider,
        string recipientEmail,
        EmailLog emailLog,
        CancellationToken cancellationToken)
    {
        try
        {
            var providerImpl = providerFactory.CreateProvider(provider);

            await providerImpl.SendAsync(
                recipientEmail,
                TestEmailSubject,
                TestEmailHtmlBody,
                TestEmailPlainTextBody,
                cancellationToken);

            await circuitBreaker.RecordSuccessAsync(provider, cancellationToken);

            logger.LogInformation(
                "Test email sent successfully to {RecipientEmail} via provider {ProviderName}",
                recipientEmail, provider.Name);

            return new TestEmailResultDto(
                Success: true,
                Error: null,
                ProviderUsedId: provider.Id,
                ProviderUsedName: provider.Name,
                AttemptCount: 1,
                TotalProvidersAttempted: 1
            );
        }
        catch (Exception ex)
        {
            await circuitBreaker.RecordFailureAsync(provider, cancellationToken);

            logger.LogWarning(ex,
                "Failed to send test email via provider {ProviderName}: {Error}",
                provider.Name, ex.Message);

            return new TestEmailResultDto(
                Success: false,
                Error: $"Provider {provider.Name}: {ex.Message}",
                ProviderUsedId: provider.Id,
                ProviderUsedName: provider.Name,
                AttemptCount: 1,
                TotalProvidersAttempted: 1
            );
        }
    }
}
