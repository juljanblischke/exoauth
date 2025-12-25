using FluentValidation;

namespace ExoAuth.Application.Features.SystemUsers.Commands.UpdateSystemUser;

public sealed class UpdateSystemUserValidator : AbstractValidator<UpdateSystemUserCommand>
{
    public UpdateSystemUserValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters")
            .When(x => x.FirstName is not null);

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters")
            .When(x => x.LastName is not null);
    }
}
