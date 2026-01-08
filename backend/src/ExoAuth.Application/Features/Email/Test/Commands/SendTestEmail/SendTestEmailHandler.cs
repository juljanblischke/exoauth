using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Email.Test.Commands.SendTestEmail;

public sealed class SendTestEmailHandler(
    IAppDbContext dbContext,
    IEncryptionService encryptionService,
    ILogger<SendTestEmailHandler> logger
) : IRequestHandler<SendTestEmailCommand, TestEmailResultDto>
{
    public async ValueTask<TestEmailResultDto> Handle(SendTestEmailCommand request, CancellationToken cancellationToken)
    {
        var providersQuery = dbContext.EmailProviders
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority);

        // If a specific provider is requested, test only that one
        if (request.ProviderId.HasValue)
        {
            var provider = await dbContext.EmailProviders
                .FirstOrDefaultAsync(p => p.Id == request.ProviderId.Value, cancellationToken);

            if (provider is null)
            {
                throw new EmailProviderNotFoundException(request.ProviderId!.Value);
            }

            // Validate configuration can be decrypted
            try
            {
                var configJson = encryptionService.Decrypt(provider.ConfigurationEncrypted);
                if (string.IsNullOrWhiteSpace(configJson))
                {
                    return new TestEmailResultDto(
                        Success: false,
                        Error: "Provider configuration is empty or corrupted",
                        ProviderUsedId: provider.Id,
                        ProviderUsedName: provider.Name,
                        AttemptCount: 1,
                        TotalProvidersAttempted: 1
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to decrypt configuration for provider {ProviderId}", provider.Id);
                return new TestEmailResultDto(
                    Success: false,
                    Error: "Failed to decrypt provider configuration",
                    ProviderUsedId: provider.Id,
                    ProviderUsedName: provider.Name,
                    AttemptCount: 1,
                    TotalProvidersAttempted: 1
                );
            }

            // Attempt to send test email
            var result = await SendTestEmailViaProviderAsync(provider.Id, provider.Name, provider.Type, request.RecipientEmail, cancellationToken);
            return result with { TotalProvidersAttempted = 1 };
        }

        // Test with failover through all enabled providers
        var providers = await providersQuery.ToListAsync(cancellationToken);

        if (providers.Count == 0)
        {
            throw new EmailNoProvidersConfiguredException();
        }

        var attemptCount = 0;
        string? lastError = null;

        foreach (var provider in providers)
        {
            attemptCount++;

            // Check circuit breaker
            if (provider.CircuitBreakerOpenUntil.HasValue && provider.CircuitBreakerOpenUntil > DateTime.UtcNow)
            {
                logger.LogDebug("Skipping provider {ProviderName} due to open circuit breaker", provider.Name);
                continue;
            }

            // Validate configuration
            try
            {
                var configJson = encryptionService.Decrypt(provider.ConfigurationEncrypted);
                if (string.IsNullOrWhiteSpace(configJson))
                {
                    lastError = $"Provider {provider.Name}: configuration is empty or corrupted";
                    continue;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to decrypt configuration for provider {ProviderId}", provider.Id);
                lastError = $"Provider {provider.Name}: failed to decrypt configuration";
                continue;
            }

            // Attempt to send
            var result = await SendTestEmailViaProviderAsync(provider.Id, provider.Name, provider.Type, request.RecipientEmail, cancellationToken);
            
            if (result.Success)
            {
                return result with { AttemptCount = attemptCount, TotalProvidersAttempted = providers.Count };
            }

            lastError = result.Error;
        }

        // All providers failed
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
        Guid providerId,
        string providerName,
        EmailProviderType providerType,
        string recipientEmail,
        CancellationToken cancellationToken)
    {
        // TODO: Implement actual email sending via provider when provider implementations are complete
        // For now, this validates the provider is configured and returns success
        // Actual provider implementations will be added in Phase 5
        
        logger.LogInformation(
            "Test email would be sent to {RecipientEmail} via provider {ProviderName} ({ProviderType})",
            recipientEmail, providerName, providerType);

        // Simulate sending - in production, this would call the actual provider
        // This placeholder allows testing the API flow while providers are being implemented
        await Task.CompletedTask;

        return new TestEmailResultDto(
            Success: true,
            Error: null,
            ProviderUsedId: providerId,
            ProviderUsedName: providerName,
            AttemptCount: 1,
            TotalProvidersAttempted: 1
        );
    }
}
