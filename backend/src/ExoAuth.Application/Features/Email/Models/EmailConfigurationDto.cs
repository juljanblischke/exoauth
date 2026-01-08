namespace ExoAuth.Application.Features.Email.Models;

/// <summary>
/// Email configuration DTO.
/// </summary>
public sealed record EmailConfigurationDto(
    Guid Id,
    // Retry Settings
    int MaxRetriesPerProvider,
    int InitialRetryDelayMs,
    int MaxRetryDelayMs,
    double BackoffMultiplier,
    // Circuit Breaker Settings
    int CircuitBreakerFailureThreshold,
    int CircuitBreakerWindowMinutes,
    int CircuitBreakerOpenDurationMinutes,
    // DLQ Settings
    bool AutoRetryDlq,
    int DlqRetryIntervalHours,
    // General Settings
    bool EmailsEnabled,
    bool TestMode,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>
/// Request to update email configuration.
/// </summary>
public sealed record UpdateEmailConfigurationRequest(
    // Retry Settings
    int MaxRetriesPerProvider,
    int InitialRetryDelayMs,
    int MaxRetryDelayMs,
    double BackoffMultiplier,
    // Circuit Breaker Settings
    int CircuitBreakerFailureThreshold,
    int CircuitBreakerWindowMinutes,
    int CircuitBreakerOpenDurationMinutes,
    // DLQ Settings
    bool AutoRetryDlq,
    int DlqRetryIntervalHours,
    // General Settings
    bool EmailsEnabled,
    bool TestMode
);
