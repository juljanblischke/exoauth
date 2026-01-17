using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.ResetPassword;

/// <summary>
/// Command to reset a user's password using a token or code.
/// Either token OR (email + code) must be provided.
/// </summary>
public sealed record ResetPasswordCommand(
    string? Token,
    string? Email,
    string? Code,
    string NewPassword
) : ICommand<ResetPasswordResponse>;

/// <summary>
/// Response for password reset.
/// </summary>
public sealed record ResetPasswordResponse(
    bool Success,
    string Message
);
