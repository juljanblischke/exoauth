using FluentValidation;

namespace ExoAuth.Application.Features.Auth.Commands.PasskeyLogin;

public sealed class PasskeyLoginValidator : AbstractValidator<PasskeyLoginCommand>
{
    public PasskeyLoginValidator()
    {
        RuleFor(x => x.ChallengeId)
            .NotEmpty()
            .WithMessage("Challenge ID is required.");

        RuleFor(x => x.AssertionResponse)
            .NotNull()
            .WithMessage("Assertion response is required.");
    }
}
