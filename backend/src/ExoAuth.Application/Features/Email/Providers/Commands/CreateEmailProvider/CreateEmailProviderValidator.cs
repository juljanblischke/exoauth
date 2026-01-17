using ExoAuth.Domain.Enums;
using FluentValidation;

namespace ExoAuth.Application.Features.Email.Providers.Commands.CreateEmailProvider;

public sealed class CreateEmailProviderValidator : AbstractValidator<CreateEmailProviderCommand>
{
    public CreateEmailProviderValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid provider type.");

        RuleFor(x => x.Priority)
            .GreaterThan(0).WithMessage("Priority must be greater than 0.");

        RuleFor(x => x.Configuration)
            .NotNull().WithMessage("Configuration is required.");

        RuleFor(x => x.Configuration.FromEmail)
            .NotEmpty().WithMessage("From email is required.")
            .EmailAddress().WithMessage("From email must be a valid email address.")
            .When(x => x.Configuration != null);

        RuleFor(x => x.Configuration.FromName)
            .NotEmpty().WithMessage("From name is required.")
            .MaximumLength(200).WithMessage("From name must not exceed 200 characters.")
            .When(x => x.Configuration != null);

        // SMTP specific validation
        When(x => x.Type == EmailProviderType.Smtp, () =>
        {
            RuleFor(x => x.Configuration.Host)
                .NotEmpty().WithMessage("SMTP host is required for SMTP providers.");
            RuleFor(x => x.Configuration.Port)
                .NotNull().WithMessage("SMTP port is required for SMTP providers.")
                .InclusiveBetween(1, 65535).WithMessage("Port must be between 1 and 65535.");
        });

        // API key providers (SendGrid, Resend, Postmark)
        When(x => x.Type == EmailProviderType.SendGrid || x.Type == EmailProviderType.Resend, () =>
        {
            RuleFor(x => x.Configuration.ApiKey)
                .NotEmpty().WithMessage("API key is required for this provider.");
        });

        // Postmark specific
        When(x => x.Type == EmailProviderType.Postmark, () =>
        {
            RuleFor(x => x.Configuration.ServerToken)
                .NotEmpty().WithMessage("Server token is required for Postmark.");
        });

        // Mailgun specific
        When(x => x.Type == EmailProviderType.Mailgun, () =>
        {
            RuleFor(x => x.Configuration.ApiKey)
                .NotEmpty().WithMessage("API key is required for Mailgun.");
            RuleFor(x => x.Configuration.Domain)
                .NotEmpty().WithMessage("Domain is required for Mailgun.");
        });

        // Amazon SES specific
        When(x => x.Type == EmailProviderType.AmazonSes, () =>
        {
            RuleFor(x => x.Configuration.AccessKey)
                .NotEmpty().WithMessage("Access key is required for Amazon SES.");
            RuleFor(x => x.Configuration.SecretKey)
                .NotEmpty().WithMessage("Secret key is required for Amazon SES.");
            RuleFor(x => x.Configuration.AwsRegion)
                .NotEmpty().WithMessage("AWS region is required for Amazon SES.");
        });
    }
}
