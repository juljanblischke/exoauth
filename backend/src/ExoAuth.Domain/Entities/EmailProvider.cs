using ExoAuth.Domain.Enums;

namespace ExoAuth.Domain.Entities;

/// <summary>
/// Represents an email provider configuration with circuit breaker and failover support.
/// </summary>
public sealed class EmailProvider : BaseEntity
{
    /// <summary>
    /// Display name for this provider (e.g., "Primary SendGrid", "Backup SMTP").
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// The type of email provider.
    /// </summary>
    public EmailProviderType Type { get; private set; }

    /// <summary>
    /// Priority for failover chain (1 = primary, 2 = first fallback, etc.).
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Whether this provider is currently enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Encrypted JSON configuration specific to the provider type.
    /// </summary>
    public string ConfigurationEncrypted { get; private set; } = null!;

    // Circuit Breaker State
    /// <summary>
    /// Number of consecutive failures for this provider.
    /// </summary>
    public int FailureCount { get; private set; }

    /// <summary>
    /// Timestamp of the last failure.
    /// </summary>
    public DateTime? LastFailureAt { get; private set; }

    /// <summary>
    /// If set, the circuit breaker is open and the provider is unavailable until this time.
    /// </summary>
    public DateTime? CircuitBreakerOpenUntil { get; private set; }

    // Statistics
    /// <summary>
    /// Total number of emails successfully sent via this provider.
    /// </summary>
    public int TotalSent { get; private set; }

    /// <summary>
    /// Total number of emails that failed to send via this provider.
    /// </summary>
    public int TotalFailed { get; private set; }

    /// <summary>
    /// Timestamp of the last successful email send.
    /// </summary>
    public DateTime? LastSuccessAt { get; private set; }

    private EmailProvider() { } // EF Core

    /// <summary>
    /// Creates a new email provider.
    /// </summary>
    public static EmailProvider Create(
        string name,
        EmailProviderType type,
        int priority,
        string configurationEncrypted,
        bool isEnabled = true)
    {
        return new EmailProvider
        {
            Name = name,
            Type = type,
            Priority = priority,
            ConfigurationEncrypted = configurationEncrypted,
            IsEnabled = isEnabled,
            FailureCount = 0,
            TotalSent = 0,
            TotalFailed = 0
        };
    }

    /// <summary>
    /// Updates the provider configuration.
    /// </summary>
    public void Update(
        string name,
        EmailProviderType type,
        int priority,
        string configurationEncrypted,
        bool isEnabled)
    {
        Name = name;
        Type = type;
        Priority = priority;
        ConfigurationEncrypted = configurationEncrypted;
        IsEnabled = isEnabled;
        SetUpdated();
    }

    /// <summary>
    /// Updates only the priority (used for reordering).
    /// </summary>
    public void SetPriority(int priority)
    {
        Priority = priority;
        SetUpdated();
    }

    /// <summary>
    /// Enables the provider.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        SetUpdated();
    }

    /// <summary>
    /// Disables the provider.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        SetUpdated();
    }

    /// <summary>
    /// Records a successful email send.
    /// </summary>
    public void RecordSuccess()
    {
        TotalSent++;
        FailureCount = 0;
        LastSuccessAt = DateTime.UtcNow;
        CircuitBreakerOpenUntil = null; // Close circuit on success
        SetUpdated();
    }

    /// <summary>
    /// Records a failed email send attempt.
    /// </summary>
    public void RecordFailure()
    {
        TotalFailed++;
        FailureCount++;
        LastFailureAt = DateTime.UtcNow;
        SetUpdated();
    }

    /// <summary>
    /// Records a failed email send attempt and opens circuit breaker if threshold exceeded.
    /// </summary>
    public void RecordFailure(int failureThreshold, int circuitBreakerOpenDurationMinutes)
    {
        TotalFailed++;
        FailureCount++;
        LastFailureAt = DateTime.UtcNow;

        if (FailureCount >= failureThreshold)
        {
            CircuitBreakerOpenUntil = DateTime.UtcNow.AddMinutes(circuitBreakerOpenDurationMinutes);
        }

        SetUpdated();
    }

    /// <summary>
    /// Opens the circuit breaker, preventing use of this provider until the specified time.
    /// </summary>
    public void OpenCircuitBreaker(int durationMinutes)
    {
        CircuitBreakerOpenUntil = DateTime.UtcNow.AddMinutes(durationMinutes);
        SetUpdated();
    }

    /// <summary>
    /// Manually resets the circuit breaker, allowing the provider to be used again.
    /// </summary>
    public void ResetCircuitBreaker()
    {
        CircuitBreakerOpenUntil = null;
        FailureCount = 0;
        SetUpdated();
    }

    /// <summary>
    /// Checks if the circuit breaker is currently open.
    /// </summary>
    public bool IsCircuitBreakerOpen => CircuitBreakerOpenUntil.HasValue && DateTime.UtcNow < CircuitBreakerOpenUntil.Value;

    /// <summary>
    /// Checks if the provider can be used (enabled and circuit breaker not open).
    /// </summary>
    public bool CanBeUsed => IsEnabled && !IsCircuitBreakerOpen;

    /// <summary>
    /// Gets the success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate
    {
        get
        {
            var total = TotalSent + TotalFailed;
            return total == 0 ? 100.0 : (double)TotalSent / total * 100.0;
        }
    }
}
