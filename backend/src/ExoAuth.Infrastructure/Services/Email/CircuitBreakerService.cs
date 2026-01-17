using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services.Email;

public sealed class CircuitBreakerService : ICircuitBreakerService
{
    private readonly IAppDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CircuitBreakerService> _logger;

    public CircuitBreakerService(
        IAppDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<CircuitBreakerService> logger)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<bool> CanUseProviderAsync(EmailProvider provider, CancellationToken cancellationToken = default)
    {
        if (!provider.IsEnabled)
            return false;

        if (provider.CircuitBreakerOpenUntil.HasValue)
        {
            if (_dateTimeProvider.UtcNow < provider.CircuitBreakerOpenUntil.Value)
            {
                _logger.LogDebug(
                    "Circuit breaker still open for provider {ProviderId} until {Until}",
                    provider.Id, provider.CircuitBreakerOpenUntil);
                return false;
            }

            // Half-open state - try again
            _logger.LogInformation(
                "Circuit breaker entering half-open state for provider {ProviderId}",
                provider.Id);
        }

        return true;
    }

    public async Task RecordSuccessAsync(EmailProvider provider, CancellationToken cancellationToken = default)
    {
        provider.RecordSuccess();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Recorded success for provider {ProviderId}, total sent: {TotalSent}",
            provider.Id, provider.TotalSent);
    }

    public async Task RecordFailureAsync(EmailProvider provider, CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(cancellationToken);

        provider.RecordFailure(
            config.CircuitBreakerFailureThreshold,
            config.CircuitBreakerOpenDurationMinutes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (provider.CircuitBreakerOpenUntil.HasValue)
        {
            _logger.LogWarning(
                "Circuit breaker opened for provider {ProviderId} until {Until}",
                provider.Id, provider.CircuitBreakerOpenUntil);
        }
        else
        {
            _logger.LogDebug(
                "Recorded failure for provider {ProviderId}, failure count: {FailureCount}",
                provider.Id, provider.FailureCount);
        }
    }

    public async Task ResetCircuitBreakerAsync(EmailProvider provider, CancellationToken cancellationToken = default)
    {
        provider.ResetCircuitBreaker();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Circuit breaker manually reset for provider {ProviderId}",
            provider.Id);
    }

    private async Task<EmailConfiguration> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        var config = await _dbContext.EmailConfigurations.FirstOrDefaultAsync(cancellationToken);
        return config ?? EmailConfiguration.CreateDefault();
    }
}
