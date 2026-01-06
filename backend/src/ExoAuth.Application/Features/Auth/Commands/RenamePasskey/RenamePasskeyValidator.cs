using FluentValidation;

namespace ExoAuth.Application.Features.Auth.Commands.RenamePasskey;

public sealed class RenamePasskeyValidator : AbstractValidator<RenamePasskeyCommand>
{
    public RenamePasskeyValidator()
    {
        RuleFor(x => x.PasskeyId)
            .NotEmpty()
            .WithMessage("Passkey ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters.");
    }
}
