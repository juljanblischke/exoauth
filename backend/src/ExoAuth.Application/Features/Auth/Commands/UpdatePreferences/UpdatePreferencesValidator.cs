using FluentValidation;

namespace ExoAuth.Application.Features.Auth.Commands.UpdatePreferences;

public sealed class UpdatePreferencesValidator : AbstractValidator<UpdatePreferencesCommand>
{
    private static readonly string[] SupportedLanguages = { "en", "de" };

    public UpdatePreferencesValidator()
    {
        RuleFor(x => x.Language)
            .NotEmpty()
            .WithMessage("Language is required")
            .Must(BeASupportedLanguage)
            .WithMessage($"Language must be one of: {string.Join(", ", SupportedLanguages)}");
    }

    private static bool BeASupportedLanguage(string language)
    {
        return SupportedLanguages.Contains(language?.ToLowerInvariant());
    }
}
