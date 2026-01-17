using FluentValidation;

namespace ExoAuth.Application.Features.Auth.Commands.PasskeyRegister;

public sealed class PasskeyRegisterValidator : AbstractValidator<PasskeyRegisterCommand>
{
    public PasskeyRegisterValidator()
    {
        RuleFor(x => x.ChallengeId)
            .NotEmpty()
            .WithMessage("Challenge ID is required.");

        RuleFor(x => x.AttestationResponse)
            .NotNull()
            .WithMessage("Attestation response is required.");

        RuleFor(x => x.Name)
            .MaximumLength(100)
            .WithMessage("Passkey name cannot exceed 100 characters.")
            .When(x => x.Name is not null);
    }
}
