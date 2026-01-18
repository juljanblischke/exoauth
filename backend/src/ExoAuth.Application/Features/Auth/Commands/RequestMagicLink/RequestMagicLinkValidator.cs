using FluentValidation;

namespace ExoAuth.Application.Features.Auth.Commands.RequestMagicLink;

public sealed class RequestMagicLinkValidator : AbstractValidator<RequestMagicLinkCommand>
{
    public RequestMagicLinkValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
