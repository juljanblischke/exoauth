using FluentValidation;

namespace ExoAuth.Application.Features.Auth.Commands.ResendPasswordReset;

public sealed class ResendPasswordResetValidator : AbstractValidator<ResendPasswordResetCommand>
{
    public ResendPasswordResetValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");
    }
}
