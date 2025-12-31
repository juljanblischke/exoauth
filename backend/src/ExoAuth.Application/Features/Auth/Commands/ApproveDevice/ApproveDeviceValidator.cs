using FluentValidation;

namespace ExoAuth.Application.Features.Auth.Commands.ApproveDevice;

public sealed class ApproveDeviceValidator : AbstractValidator<ApproveDeviceCommand>
{
    public ApproveDeviceValidator()
    {
        RuleFor(x => x.ApprovalToken)
            .NotEmpty().WithMessage("Approval token is required");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Approval code is required")
            .Matches(@"^[A-Z0-9]{4}-?[A-Z0-9]{4}$").WithMessage("Invalid code format. Expected XXXX-XXXX");
    }
}
