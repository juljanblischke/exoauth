using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Configuration.Commands.UpdateEmailConfiguration;

public sealed class UpdateEmailConfigurationHandler : ICommandHandler<UpdateEmailConfigurationCommand, EmailConfigurationDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;

    public UpdateEmailConfigurationHandler(IAppDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async ValueTask<EmailConfigurationDto> Handle(UpdateEmailConfigurationCommand command, CancellationToken ct)
    {
        var config = await _dbContext.EmailConfigurations.FirstOrDefaultAsync(ct);

        // Create default configuration if none exists
        if (config is null)
        {
            config = EmailConfiguration.CreateDefault();
            _dbContext.EmailConfigurations.Add(config);
        }

        // Update all settings
        config.UpdateAll(
            command.MaxRetriesPerProvider,
            command.InitialRetryDelayMs,
            command.MaxRetryDelayMs,
            command.BackoffMultiplier,
            command.CircuitBreakerFailureThreshold,
            command.CircuitBreakerWindowMinutes,
            command.CircuitBreakerOpenDurationMinutes,
            command.AutoRetryDlq,
            command.DlqRetryIntervalHours,
            command.EmailsEnabled,
            command.TestMode);

        await _dbContext.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            "EMAIL_CONFIGURATION_UPDATED",
            userId: null,
            targetUserId: null,
            entityType: "EmailConfiguration",
            entityId: config.Id,
            details: new
            {
                command.MaxRetriesPerProvider,
                command.InitialRetryDelayMs,
                command.MaxRetryDelayMs,
                command.BackoffMultiplier,
                command.CircuitBreakerFailureThreshold,
                command.CircuitBreakerWindowMinutes,
                command.CircuitBreakerOpenDurationMinutes,
                command.AutoRetryDlq,
                command.DlqRetryIntervalHours,
                command.EmailsEnabled,
                command.TestMode
            },
            cancellationToken: ct);

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
