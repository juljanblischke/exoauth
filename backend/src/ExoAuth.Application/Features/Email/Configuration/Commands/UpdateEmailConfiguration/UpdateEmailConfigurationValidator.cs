using FluentValidation;

namespace ExoAuth.Application.Features.Email.Configuration.Commands.UpdateEmailConfiguration;

public sealed class UpdateEmailConfigurationValidator : AbstractValidator<UpdateEmailConfigurationCommand>
{
    public UpdateEmailConfigurationValidator()
    {
        // Retry settings
        RuleFor(x => x.MaxRetriesPerProvider)
            .InclusiveBetween(0, 10).WithMessage("Max retries per provider must be between 0 and 10.");

        RuleFor(x => x.InitialRetryDelayMs)
            .InclusiveBetween(100, 60000).WithMessage("Initial retry delay must be between 100ms and 60000ms.");

        RuleFor(x => x.MaxRetryDelayMs)
            .InclusiveBetween(1000, 300000).WithMessage("Max retry delay must be between 1000ms and 300000ms.")
            .GreaterThanOrEqualTo(x => x.InitialRetryDelayMs).WithMessage("Max retry delay must be >= initial retry delay.");

        RuleFor(x => x.BackoffMultiplier)
            .InclusiveBetween(1.0, 5.0).WithMessage("Backoff multiplier must be between 1.0 and 5.0.");

        // Circuit breaker settings
        RuleFor(x => x.CircuitBreakerFailureThreshold)
            .InclusiveBetween(1, 100).WithMessage("Circuit breaker failure threshold must be between 1 and 100.");

        RuleFor(x => x.CircuitBreakerWindowMinutes)
            .InclusiveBetween(1, 1440).WithMessage("Circuit breaker window must be between 1 and 1440 minutes (24 hours).");

        RuleFor(x => x.CircuitBreakerOpenDurationMinutes)
            .InclusiveBetween(1, 1440).WithMessage("Circuit breaker open duration must be between 1 and 1440 minutes (24 hours).");

        // DLQ settings
        RuleFor(x => x.DlqRetryIntervalHours)
            .InclusiveBetween(1, 168).WithMessage("DLQ retry interval must be between 1 and 168 hours (7 days).");
    }
}
