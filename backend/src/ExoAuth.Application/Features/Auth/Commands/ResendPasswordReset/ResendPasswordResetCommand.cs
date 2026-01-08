using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.ResendPasswordReset;

/// <summary>
/// Command to resend a password reset email.
/// </summary>
public sealed record ResendPasswordResetCommand(
    string Email,
    string? CaptchaToken = null,
    string? IpAddress = null
) : ICommand<ResendPasswordResetResponse>;

/// <summary>
/// Response for resend password reset request.
/// Always returns success to prevent email enumeration.
/// </summary>
public sealed record ResendPasswordResetResponse(
    bool Success,
    string Message
);
