namespace ExoAuth.Domain.Entities;

/// <summary>
/// Global email configuration settings (singleton - only one row in database).
/// </summary>
public sealed class EmailConfiguration : BaseEntity
{
    // Retry Settings
    /// <summary>
    /// Maximum number of retries per provider before moving to the next.
    /// </summary>
    public int MaxRetriesPerProvider { get; private set; }

    /// <summary>
    /// Initial delay in milliseconds before first retry.
    /// </summary>
    public int InitialRetryDelayMs { get; private set; }

    /// <summary>
    /// Maximum delay in milliseconds between retries.
    /// </summary>
    public int MaxRetryDelayMs { get; private set; }

    /// <summary>
    /// Multiplier for exponential backoff (e.g., 2.0 = double each retry).
    /// </summary>
    public double BackoffMultiplier { get; private set; }

    // Circuit Breaker Settings
    /// <summary>
    /// Number of failures before opening the circuit breaker.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; private set; }

    /// <summary>
    /// Time window in minutes for counting failures.
    /// </summary>
    public int CircuitBreakerWindowMinutes { get; private set; }

    /// <summary>
    /// Duration in minutes to keep the circuit breaker open.
    /// </summary>
    public int CircuitBreakerOpenDurationMinutes { get; private set; }

    // DLQ Settings
    /// <summary>
    /// Whether to automatically retry emails from the DLQ.
    /// </summary>
    public bool AutoRetryDlq { get; private set; }

    /// <summary>
    /// Interval in hours between DLQ retry attempts.
    /// </summary>
    public int DlqRetryIntervalHours { get; private set; }

    // General Settings
    /// <summary>
    /// Global on/off switch for all email sending.
    /// </summary>
    public bool EmailsEnabled { get; private set; }

    /// <summary>
    /// Test mode - log emails but don't actually send them.
    /// </summary>
    public bool TestMode { get; private set; }

    private EmailConfiguration() { } // EF Core

    /// <summary>
    /// Creates a new email configuration with default values.
    /// </summary>
    public static EmailConfiguration CreateDefault()
    {
        return new EmailConfiguration
        {
            // Retry defaults
            MaxRetriesPerProvider = 3,
            InitialRetryDelayMs = 1000,
            MaxRetryDelayMs = 60000,
            BackoffMultiplier = 2.0,

            // Circuit breaker defaults
            CircuitBreakerFailureThreshold = 5,
            CircuitBreakerWindowMinutes = 10,
            CircuitBreakerOpenDurationMinutes = 30,

            // DLQ defaults
            AutoRetryDlq = false,
            DlqRetryIntervalHours = 6,

            // General defaults
            EmailsEnabled = true,
            TestMode = false
        };
    }

    /// <summary>
    /// Updates the retry settings.
    /// </summary>
    public void UpdateRetrySettings(
        int maxRetriesPerProvider,
        int initialRetryDelayMs,
        int maxRetryDelayMs,
        double backoffMultiplier)
    {
        MaxRetriesPerProvider = maxRetriesPerProvider;
        InitialRetryDelayMs = initialRetryDelayMs;
        MaxRetryDelayMs = maxRetryDelayMs;
        BackoffMultiplier = backoffMultiplier;
        SetUpdated();
    }

    /// <summary>
    /// Updates the circuit breaker settings.
    /// </summary>
    public void UpdateCircuitBreakerSettings(
        int failureThreshold,
        int windowMinutes,
        int openDurationMinutes)
    {
        CircuitBreakerFailureThreshold = failureThreshold;
        CircuitBreakerWindowMinutes = windowMinutes;
        CircuitBreakerOpenDurationMinutes = openDurationMinutes;
        SetUpdated();
    }

    /// <summary>
    /// Updates the DLQ settings.
    /// </summary>
    public void UpdateDlqSettings(
        bool autoRetry,
        int retryIntervalHours)
    {
        AutoRetryDlq = autoRetry;
        DlqRetryIntervalHours = retryIntervalHours;
        SetUpdated();
    }

    /// <summary>
    /// Updates the general settings.
    /// </summary>
    public void UpdateGeneralSettings(
        bool emailsEnabled,
        bool testMode)
    {
        EmailsEnabled = emailsEnabled;
        TestMode = testMode;
        SetUpdated();
    }

    /// <summary>
    /// Updates all settings at once.
    /// </summary>
    public void UpdateAll(
        int maxRetriesPerProvider,
        int initialRetryDelayMs,
        int maxRetryDelayMs,
        double backoffMultiplier,
        int circuitBreakerFailureThreshold,
        int circuitBreakerWindowMinutes,
        int circuitBreakerOpenDurationMinutes,
        bool autoRetryDlq,
        int dlqRetryIntervalHours,
        bool emailsEnabled,
        bool testMode)
    {
        MaxRetriesPerProvider = maxRetriesPerProvider;
        InitialRetryDelayMs = initialRetryDelayMs;
        MaxRetryDelayMs = maxRetryDelayMs;
        BackoffMultiplier = backoffMultiplier;
        CircuitBreakerFailureThreshold = circuitBreakerFailureThreshold;
        CircuitBreakerWindowMinutes = circuitBreakerWindowMinutes;
        CircuitBreakerOpenDurationMinutes = circuitBreakerOpenDurationMinutes;
        AutoRetryDlq = autoRetryDlq;
        DlqRetryIntervalHours = dlqRetryIntervalHours;
        EmailsEnabled = emailsEnabled;
        TestMode = testMode;
        SetUpdated();
    }

    /// <summary>
    /// Calculates the backoff delay for a given retry attempt.
    /// </summary>
    public int CalculateBackoffDelay(int retryAttempt)
    {
        var delay = InitialRetryDelayMs * Math.Pow(BackoffMultiplier, retryAttempt);
        return Math.Min((int)delay, MaxRetryDelayMs);
    }
}
