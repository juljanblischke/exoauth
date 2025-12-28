using FluentValidation;

namespace ExoAuth.Application.Features.Auth.Commands.RegenerateBackupCodes;

public sealed class RegenerateBackupCodesValidator : AbstractValidator<RegenerateBackupCodesCommand>
{
    public RegenerateBackupCodesValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("MFA code is required")
            .Length(6)
            .WithMessage("MFA code must be 6 digits")
            .Matches(@"^\d{6}$")
            .WithMessage("MFA code must contain only digits");
    }
}
