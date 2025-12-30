using FluentValidation;

namespace ExoAuth.Application.Features.SystemInvites.Commands.UpdateInvite;

public sealed class UpdateInviteValidator : AbstractValidator<UpdateInviteCommand>
{
    public UpdateInviteValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Invite ID is required");

        When(x => x.FirstName is not null, () =>
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name cannot be empty")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters");
        });

        When(x => x.LastName is not null, () =>
        {
            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name cannot be empty")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");
        });

        When(x => x.PermissionIds is not null, () =>
        {
            RuleFor(x => x.PermissionIds)
                .NotEmpty().WithMessage("At least one permission is required when updating permissions");
        });

        RuleFor(x => x)
            .Must(x => x.FirstName is not null || x.LastName is not null || x.PermissionIds is not null)
            .WithMessage("At least one field must be provided for update");
    }
}
