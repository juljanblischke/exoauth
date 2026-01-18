using FluentValidation;

namespace ExoAuth.Application.Features.Auth.Commands.LoginWithMagicLink;

public sealed class LoginWithMagicLinkValidator : AbstractValidator<LoginWithMagicLinkCommand>
{
    public LoginWithMagicLinkValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Magic link token is required");
    }
}
