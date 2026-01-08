using FluentValidation;

namespace ExoAuth.Application.Features.Email.Test.Commands.SendTestEmail;

public sealed class SendTestEmailCommandValidator : AbstractValidator<SendTestEmailCommand>
{
    public SendTestEmailCommandValidator()
    {
        RuleFor(x => x.RecipientEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}
