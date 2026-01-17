using FluentValidation;

namespace ExoAuth.Application.Features.Email.Providers.Commands.ReorderProviders;

public sealed class ReorderProvidersValidator : AbstractValidator<ReorderProvidersCommand>
{
    public ReorderProvidersValidator()
    {
        RuleFor(x => x.Providers)
            .NotEmpty().WithMessage("At least one provider is required.");

        RuleForEach(x => x.Providers).ChildRules(provider =>
        {
            provider.RuleFor(p => p.ProviderId)
                .NotEmpty().WithMessage("Provider ID is required.");

            provider.RuleFor(p => p.Priority)
                .GreaterThan(0).WithMessage("Priority must be greater than 0.");
        });
    }
}
