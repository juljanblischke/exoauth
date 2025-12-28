using FluentValidation;

namespace ExoAuth.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        // Either token OR (email + code) must be provided
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.Token) || (!string.IsNullOrEmpty(x.Email) && !string.IsNullOrEmpty(x.Code)))
            .WithMessage("Either token or email with code must be provided");

        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format");
        });

        When(x => !string.IsNullOrEmpty(x.Code), () =>
        {
            RuleFor(x => x.Code)
                .Matches(@"^[A-Z0-9]{4}-?[A-Z0-9]{4}$").WithMessage("Invalid code format. Expected XXXX-XXXX");
        });
    }
}
