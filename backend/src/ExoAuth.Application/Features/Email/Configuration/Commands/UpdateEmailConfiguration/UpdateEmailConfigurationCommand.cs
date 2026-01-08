using ExoAuth.Application.Features.Email.Models;
using Mediator;

namespace ExoAuth.Application.Features.Email.Configuration.Commands.UpdateEmailConfiguration;

/// <summary>
/// Command to update the email configuration.
/// </summary>
public sealed record UpdateEmailConfigurationCommand(
    int MaxRetriesPerProvider,
    int InitialRetryDelayMs,
    int MaxRetryDelayMs,
    double BackoffMultiplier,
    int CircuitBreakerFailureThreshold,
    int CircuitBreakerWindowMinutes,
    int CircuitBreakerOpenDurationMinutes,
    bool AutoRetryDlq,
    int DlqRetryIntervalHours,
    bool EmailsEnabled,
    bool TestMode
) : ICommand<EmailConfigurationDto>;
