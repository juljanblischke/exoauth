using ExoAuth.Domain.Enums;
using FluentValidation;

namespace ExoAuth.Application.Features.Email.Announcements.Commands.CreateEmailAnnouncement;

public sealed class CreateEmailAnnouncementCommandValidator : AbstractValidator<CreateEmailAnnouncementCommand>
{
    public CreateEmailAnnouncementCommandValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.HtmlBody)
            .NotEmpty()
            .MaximumLength(100000);

        RuleFor(x => x.PlainTextBody)
            .MaximumLength(50000)
            .When(x => x.PlainTextBody is not null);

        RuleFor(x => x.TargetType)
            .IsInEnum();

        RuleFor(x => x.TargetPermission)
            .NotEmpty()
            .MaximumLength(128)
            .When(x => x.TargetType == EmailAnnouncementTarget.ByPermission);

        RuleFor(x => x.TargetUserIds)
            .NotEmpty()
            .Must(ids => ids is not null && ids.Count > 0)
            .WithMessage("At least one user ID must be provided when targeting selected users.")
            .When(x => x.TargetType == EmailAnnouncementTarget.SelectedUsers);

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty();
    }
}
