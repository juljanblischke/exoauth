using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.ForgotPassword;

/// <summary>
/// Command to request a password reset email.
/// </summary>
public sealed record ForgotPasswordCommand(
    string Email
) : ICommand<ForgotPasswordResponse>;

/// <summary>
/// Response for forgot password request.
/// Always returns success to prevent email enumeration.
/// </summary>
public sealed record ForgotPasswordResponse(
    bool Success,
    string Message
);
