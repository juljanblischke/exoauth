using FluentValidation;

namespace ExoAuth.Application.Features.Auth.Commands.ResendDeviceApproval;

public sealed class ResendDeviceApprovalValidator : AbstractValidator<ResendDeviceApprovalCommand>
{
    public ResendDeviceApprovalValidator()
    {
        RuleFor(x => x.ApprovalToken)
            .NotEmpty().WithMessage("Approval token is required")
            .MaximumLength(255).WithMessage("Approval token must not exceed 255 characters");
    }
}
