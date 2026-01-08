using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Configuration.Queries.GetEmailConfiguration;

public sealed class GetEmailConfigurationHandler : IQueryHandler<GetEmailConfigurationQuery, EmailConfigurationDto>
{
    private readonly IAppDbContext _dbContext;

    public GetEmailConfigurationHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<EmailConfigurationDto> Handle(GetEmailConfigurationQuery query, CancellationToken ct)
    {
        var config = await _dbContext.EmailConfigurations.FirstOrDefaultAsync(ct);

        // Create default configuration if none exists
        if (config is null)
        {
            config = EmailConfiguration.CreateDefault();
            _dbContext.EmailConfigurations.Add(config);
            await _dbContext.SaveChangesAsync(ct);
        }

        return new EmailConfigurationDto(
            config.Id,
            config.MaxRetriesPerProvider,
            config.InitialRetryDelayMs,
            config.MaxRetryDelayMs,
            config.BackoffMultiplier,
            config.CircuitBreakerFailureThreshold,
            config.CircuitBreakerWindowMinutes,
            config.CircuitBreakerOpenDurationMinutes,
            config.AutoRetryDlq,
            config.DlqRetryIntervalHours,
            config.EmailsEnabled,
            config.TestMode,
            config.CreatedAt,
            config.UpdatedAt);
    }
}
