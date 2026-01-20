using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.RequestMagicLink;

/// <summary>
/// Command to request a magic link email for passwordless login.
/// </summary>
public sealed record RequestMagicLinkCommand(
    string Email,
    string? CaptchaToken = null,
    string? IpAddress = null
) : ICommand<RequestMagicLinkResponse>;

/// <summary>
/// Response for magic link request.
/// Always returns success to prevent email enumeration.
/// </summary>
public sealed record RequestMagicLinkResponse(
    bool Success,
    string Message
);
