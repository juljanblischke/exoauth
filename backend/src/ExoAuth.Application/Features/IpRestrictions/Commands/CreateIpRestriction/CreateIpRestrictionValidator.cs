using FluentValidation;

namespace ExoAuth.Application.Features.IpRestrictions.Commands.CreateIpRestriction;

public sealed class CreateIpRestrictionValidator : AbstractValidator<CreateIpRestrictionCommand>
{
    public CreateIpRestrictionValidator()
    {
        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("IP address is required.")
            .MaximumLength(50).WithMessage("IP address must not exceed 50 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid restriction type.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");

        RuleFor(x => x.ExpiresAt)
            .Must(BeInFuture).When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiration date must be in the future.");
    }

    private static bool BeInFuture(DateTime? date)
    {
        return date == null || date > DateTime.UtcNow;
    }
}
