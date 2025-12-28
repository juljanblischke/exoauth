using FluentValidation;

namespace ExoAuth.Application.Features.Auth.Commands.MfaVerify;

public sealed class MfaVerifyValidator : AbstractValidator<MfaVerifyCommand>
{
    public MfaVerifyValidator()
    {
        RuleFor(x => x.MfaToken)
            .NotEmpty()
            .WithMessage("MFA token is required");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("MFA code is required")
            .MinimumLength(6)
            .WithMessage("MFA code must be at least 6 characters");
    }
}
